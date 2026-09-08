using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MoojPay.Domain.Entities;
using MoojPay.Domain.Enums;
using MoojPay.Modules.Marketplace.Contracts;
using MoojPay.Modules.Marketplace.Firebase;
using MoojPay.Modules.Marketplace.Persistence;
using MoojPay.Modules.Marketplace.Zaps;

namespace MoojPay.Modules.Marketplace.Services;

/// <summary>
/// Orchestrates the marketplace checkout flow: resolves offers from Zaps, redeems each cart
/// line, persists MoojPay's own <see cref="MarketplaceOrder"/> record, and triggers the
/// post-purchase push notification. Implements planned impacts on
/// chg_01a07fe56a137406b460744aa1dbf531.
/// </summary>
public sealed class MarketplaceOrderService(
    MarketplaceDbContext dbContext,
    IZapsClient zapsClient,
    IFirebaseClient firebaseClient,
    ILogger<MarketplaceOrderService> logger)
{
    public async Task<MarketplaceOffersPageDto> ListOffersAsync(
        int pageNumber,
        int pageSize,
        int? categoryId,
        CancellationToken cancellationToken)
    {
        var offers = await zapsClient.ListOffersAsync(pageNumber, pageSize, categoryId, cancellationToken)
            .ConfigureAwait(false);

        var items = offers.Select(MapOffer).ToList();
        return new MarketplaceOffersPageDto(items, pageNumber, pageSize, items.Count);
    }

    /// <summary>
    /// Redeems a full cart in one checkout call. <paramref name="routeOfferId"/> must match the
    /// first cart line's offerId (routing continuity with the TRD's single-offer shape; see
    /// contract ctr_01a07c55ed437c568e493b9a8e942d18). <paramref name="paymentReference"/> is the
    /// opaque Bank BaaS charge reference from the mobile app's embedded Bank SDK — this method
    /// never receives or stores a card number, PIN, or wallet credential, preserving the
    /// NEO PCI-DSS boundary (acr_01a07bf5813f74a09ca8585c604b1109).
    /// </summary>
    public async Task<RedeemOutcome> RedeemCartAsync(
        string routeOfferId,
        RedeemCartRequest request,
        string customerId,
        string? deviceToken,
        CancellationToken cancellationToken)
    {
        if (request.Items.Count == 0)
        {
            return RedeemOutcome.Validation("The cart must contain at least one item.");
        }

        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            return RedeemOutcome.Validation("idempotencyKey is required.");
        }

        if (!string.Equals(request.Items[0].OfferId, routeOfferId, StringComparison.Ordinal))
        {
            return RedeemOutcome.Validation("The first cart line's offerId must match the route {id}.");
        }

        // Idempotent replay: a retried checkout submission with the same key returns the
        // already-created order instead of redeeming twice (Zaps RedeemOffer carries no dedup
        // key of its own; see IZapsClient.RedeemOfferAsync).
        var existingOrder = await dbContext.MarketplaceOrders
            .SingleOrDefaultAsync(o => o.IdempotencyKey == request.IdempotencyKey, cancellationToken)
            .ConfigureAwait(false);
        if (existingOrder is not null)
        {
            return RedeemOutcome.Accepted(MapOrder(existingOrder));
        }

        // The accepted Zaps contract exposes ListOffers and RedeemOffer but no GetOffer-by-id
        // operation, so per-line pricing/title is resolved by listing the catalog and matching by
        // id. This is a pragmatic choice within the accepted contract's operation set, not a new
        // Zaps call.
        var catalog = await zapsClient.ListOffersAsync(0, 200, null, cancellationToken).ConfigureAwait(false);
        var catalogById = catalog.ToDictionary(o => o.Id, StringComparer.Ordinal);

        var resolvedItems = new List<MarketplaceOrderItem>(request.Items.Count);
        foreach (var line in request.Items)
        {
            if (line.Quantity < 1)
            {
                return RedeemOutcome.Validation($"Quantity for offer '{line.OfferId}' must be at least 1.");
            }

            if (!catalogById.TryGetValue(line.OfferId, out var offer))
            {
                return RedeemOutcome.Conflict($"Offer '{line.OfferId}' is unavailable or sold out.");
            }

            resolvedItems.Add(new MarketplaceOrderItem(line.OfferId, line.Quantity, offer.Price, offer.Title));
        }

        // Redeem each distinct cart line against Zaps (one RedeemOfferAsync call per line; see
        // the BuildSpec-level choice documented on ctr_01a07c55ed437c568e493b9a8e942d18).
        // Assumption: neither Zaps Store mode nor PIN mode is selected here (StoreId/StorePin are
        // both null) because the accepted mobile<->backend contract does not yet carry a
        // store/PIN selection; this is a known gap for a future BuildSpec refinement, not silently
        // hidden.
        string? lastZapsReference = null;
        foreach (var item in resolvedItems)
        {
            var offer = catalogById[item.OfferId];
            var redeemRequest = new ZapsRedeemOfferRequest(
                CouponId: long.TryParse(new string(item.OfferId.Where(char.IsDigit).ToArray()), out var couponId) ? couponId : 0,
                CouponCode: item.OfferId,
                UniqueIdentifier: customerId,
                RedemptionType: offer.RedemptionMethodType ?? "voucher",
                StoreId: null,
                StorePin: null);

            var result = await zapsClient.RedeemOfferAsync(redeemRequest, cancellationToken).ConfigureAwait(false);
            if (result is null)
            {
                logger.LogWarning("Zaps RedeemOffer returned no result for offer {OfferId}", item.OfferId);
                return RedeemOutcome.Conflict($"Offer '{item.OfferId}' is unavailable or sold out.");
            }

            lastZapsReference = result.RedemptionId ?? lastZapsReference;
        }

        var now = DateTimeOffset.UtcNow;
        var order = new MarketplaceOrder
        {
            Id = Guid.NewGuid(),
            Status = MarketplaceOrderStatus.Fulfilled,
            Items = resolvedItems,
            TotalAmount = resolvedItems.Sum(i => i.UnitPrice * i.Quantity),
            Currency = "SAR",
            CreatedAt = now,
            UpdatedAt = now,
            ZapsPurchaseReference = lastZapsReference,
            IdempotencyKey = request.IdempotencyKey
        };

        dbContext.MarketplaceOrders.Add(order);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Marketplace order {OrderId} fulfilled for customer {CustomerId} via paymentReference {PaymentReference}",
            order.Id,
            customerId,
            request.PaymentReference);

        // Post-purchase push confirmation (acr_01a07bf5813f75fd9a02aea22fe57bbf). A push failure
        // never rolls back an already-successful, already-persisted payment/order.
        if (!string.IsNullOrWhiteSpace(deviceToken))
        {
            try
            {
                await firebaseClient.SendPushNotificationAsync(
                    new FirebasePushRequest(
                        deviceToken,
                        new FirebaseNotification("Purchase confirmed", "Your marketplace order is confirmed."),
                        new Dictionary<string, string> { ["orderId"] = order.Id.ToString() }),
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Push confirmation failed for order {OrderId}; order remains fulfilled.", order.Id);
            }
        }
        else
        {
            logger.LogInformation("No device token supplied; skipping push confirmation for order {OrderId}.", order.Id);
        }

        return RedeemOutcome.Accepted(MapOrder(order));
    }

    public async Task<MarketplaceOrderDto?> GetOrderAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var order = await dbContext.MarketplaceOrders
            .AsNoTracking()
            .SingleOrDefaultAsync(o => o.Id == orderId, cancellationToken)
            .ConfigureAwait(false);

        return order is null ? null : MapOrder(order);
    }

    /// <summary>
    /// MoojPay processes the refund/cancellation request itself (req_01a07bf5813f7d35a32179b75fea489a),
    /// rather than deferring entirely to Zaps. Not exposed as a new public HTTP endpoint: the
    /// accepted contract ctr_01a07c55ed437c568e493b9a8e942d18 does not declare one for this
    /// change, so this is a module-level capability for now, invoked by a support/admin workflow
    /// outside this change's scope. CancelRedemption always runs Mock-only via
    /// <see cref="Zaps.ZapsClientRouter"/>.
    /// </summary>
    public async Task<MarketplaceOrderDto?> CancelOrderAsync(Guid orderId, string reason, CancellationToken cancellationToken)
    {
        var order = await dbContext.MarketplaceOrders
            .SingleOrDefaultAsync(o => o.Id == orderId, cancellationToken)
            .ConfigureAwait(false);

        if (order is null)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(order.ZapsPurchaseReference))
        {
            await zapsClient.CancelRedemptionAsync(
                new ZapsCancelRedemptionRequest(order.ZapsPurchaseReference, order.Id.ToString(), null, reason),
                cancellationToken).ConfigureAwait(false);
        }

        order.Status = MarketplaceOrderStatus.Refunded;
        order.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return MapOrder(order);
    }

    private static MarketplaceOfferDto MapOffer(ZapsOffer offer) => new(
        offer.Id,
        offer.Title,
        offer.Price,
        offer.Currency,
        offer.Category,
        offer.ImageUrl,
        offer.StartDate,
        offer.EndDate,
        Description: null,
        offer.MerchantName,
        offer.Quantity,
        offer.TermsAndConditions,
        offer.RedemptionMethodType);

    private static MarketplaceOrderDto MapOrder(MarketplaceOrder order) => new(
        order.Id.ToString(),
        order.Status.ToString().ToLowerInvariant(),
        order.Items.Select(i => new MarketplaceOrderItemDto(i.OfferId, i.Quantity, i.UnitPrice, i.Title)).ToList(),
        order.TotalAmount,
        order.Currency,
        order.CreatedAt,
        order.UpdatedAt,
        order.ZapsPurchaseReference);
}
