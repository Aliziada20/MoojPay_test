using Microsoft.Extensions.Logging;

namespace MoojPay.Modules.Marketplace.Zaps;

/// <summary>
/// Deterministic in-memory Zaps stand-in used in Mock mode (the default) and always used for
/// CancelRedemption via <see cref="ZapsClientRouter"/>, since Zaps has no confirmed real cancel
/// endpoint.
/// </summary>
public sealed class MockZapsClient(ILogger<MockZapsClient> logger) : IZapsClient
{
    private static readonly IReadOnlyList<ZapsOffer> SeedOffers =
    [
        new ZapsOffer("offer-1001", "10% off Café Bunn", 25.00m, "SAR", "dining", null, null, null, "Café Bunn", 100, "Valid at participating branches.", "voucher"),
        new ZapsOffer("offer-1002", "Free delivery voucher", 0.00m, "SAR", "delivery", null, null, null, "QuickEats", 500, "One per customer.", "code"),
        new ZapsOffer("offer-1003", "20% off electronics", 199.00m, "SAR", "retail", null, null, null, "TechMart", 50, "Excludes clearance items.", "voucher")
    ];

    public Task<IReadOnlyList<ZapsOffer>> ListOffersAsync(
        int pageNumber,
        int pageSize,
        int? categoryId,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Mock Zaps ListOffers page={PageNumber} size={PageSize} category={CategoryId}",
            pageNumber,
            pageSize,
            categoryId);

        var page = SeedOffers.Skip(pageNumber * pageSize).Take(pageSize).ToList();
        return Task.FromResult<IReadOnlyList<ZapsOffer>>(page);
    }

    public Task<ZapsRedemptionResult?> RedeemOfferAsync(
        ZapsRedeemOfferRequest request,
        CancellationToken cancellationToken)
    {
        var offerExists = SeedOffers.Any(o => o.Id == request.CouponCode || o.Id.EndsWith(request.CouponId.ToString()));

        logger.LogInformation(
            "Mock Zaps RedeemOffer coupon={CouponCode} customer={UniqueIdentifier} found={Found}",
            request.CouponCode,
            request.UniqueIdentifier,
            offerExists);

        if (!offerExists)
        {
            return Task.FromResult<ZapsRedemptionResult?>(null);
        }

        return Task.FromResult<ZapsRedemptionResult?>(
            new ZapsRedemptionResult("Redeemed", $"mock-redemption-{Guid.NewGuid():N}"));
    }

    public Task<ZapsCancelRedemptionResult> CancelRedemptionAsync(
        ZapsCancelRedemptionRequest request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Mock Zaps CancelRedemption redemption={RedemptionId} customer={UniqueIdentifier} reason={Reason}",
            request.RedemptionId,
            request.UniqueIdentifier,
            request.Reason);

        return Task.FromResult(new ZapsCancelRedemptionResult("Cancelled"));
    }
}
