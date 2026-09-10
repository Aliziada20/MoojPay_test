namespace MoojPay.Modules.Marketplace.Zaps;

/// <summary>
/// Formatted Zaps offer, per the reviewed Zaps Integration reference (§4 Offers
/// ListNearbyOffersAsync response) and contract ctr_01a07c565b127fd4a81217917541f8b9.
/// </summary>
public sealed record ZapsOffer(
    string Id,
    string Title,
    decimal Price,
    string Currency,
    string? Category,
    string? ImageUrl,
    string? StartDate,
    string? EndDate,
    string? MerchantName,
    int Quantity,
    string? TermsAndConditions,
    string? RedemptionMethodType);

public sealed record ZapsRedeemOfferRequest(
    long CouponId,
    string CouponCode,
    string UniqueIdentifier,
    string RedemptionType,
    int? StoreId,
    string? StorePin);

/// <summary>Nullable per RealZapsClient: a Zaps 404 or null data payload maps to a null result.</summary>
public sealed record ZapsRedemptionResult(string? Status, string? RedemptionId);

public sealed record ZapsCancelRedemptionRequest(
    string RedemptionId,
    string UniqueIdentifier,
    string? CouponCode,
    string? Reason);

/// <summary>
/// Hypothetical shape — CancelRedemption is not a confirmed Zaps API (see contract
/// ctr_01a07c565b127fd4a81217917541f8b9). Callers only ever observe this via the Mock
/// implementation today; see <see cref="ZapsClientRouter"/>.
/// </summary>
public sealed record ZapsCancelRedemptionResult(string Status);
