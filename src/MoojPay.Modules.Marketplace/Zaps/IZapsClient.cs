namespace MoojPay.Modules.Marketplace.Zaps;

/// <summary>
/// Backend API -&gt; Zaps boundary (contract ctr_01a07c565b127fd4a81217917541f8b9, provider
/// component cmp_01a07bff251270bbacec71e3bf47eb27). ListOffers and RedeemOffer are confirmed
/// Zaps endpoints; CancelRedemption is a placeholder shape, not a confirmed Zaps API.
/// </summary>
public interface IZapsClient
{
    /// <summary>Confirmed: GET customers/fetchNearByCoupon (Zaps ListNearbyOffersAsync).</summary>
    Task<IReadOnlyList<ZapsOffer>> ListOffersAsync(
        int pageNumber,
        int pageSize,
        int? categoryId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Confirmed: POST customers/redeemCoupon (Zaps RedeemOfferAsync). Not documented as
    /// retry-safe by Zaps; callers must not blindly retry a failed call without their own
    /// idempotency guard (see <c>MarketplaceOrder.IdempotencyKey</c>).
    /// </summary>
    Task<ZapsRedemptionResult?> RedeemOfferAsync(
        ZapsRedeemOfferRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// UNCONFIRMED Zaps operation. Every caller reaches this only through
    /// <see cref="ZapsClientRouter"/>, which forces this call to the Mock implementation
    /// regardless of configuration, because Zaps has no confirmed real cancel endpoint yet.
    /// </summary>
    Task<ZapsCancelRedemptionResult> CancelRedemptionAsync(
        ZapsCancelRedemptionRequest request,
        CancellationToken cancellationToken);
}
