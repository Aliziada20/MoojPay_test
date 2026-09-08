using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MoojPay.Modules.Marketplace.Zaps;

/// <summary>
/// The <see cref="IZapsClient"/> actually registered in DI. Routes ListOffers/RedeemOffer to the
/// Real or Mock implementation per <see cref="ZapsOptions.Mode"/>, but always forces
/// CancelRedemption to the Mock implementation: Zaps has no confirmed real cancel endpoint yet
/// (BuildSpec bsp_01a08017577d72d1a4f55d4f4ce4ac00 security notes), so a Real-mode configuration
/// must never reach a live cancel call.
/// </summary>
public sealed class ZapsClientRouter(
    [FromKeyedServices("zaps-real")] IZapsClient realClient,
    [FromKeyedServices("zaps-mock")] IZapsClient mockClient,
    IOptions<ZapsOptions> options,
    ILogger<ZapsClientRouter> logger) : IZapsClient
{
    public const string RealKey = "zaps-real";
    public const string MockKey = "zaps-mock";

    private IZapsClient Active => options.Value.Mode == ZapsClientMode.Real ? realClient : mockClient;

    public Task<IReadOnlyList<ZapsOffer>> ListOffersAsync(
        int pageNumber,
        int pageSize,
        int? categoryId,
        CancellationToken cancellationToken)
        => Active.ListOffersAsync(pageNumber, pageSize, categoryId, cancellationToken);

    public Task<ZapsRedemptionResult?> RedeemOfferAsync(
        ZapsRedeemOfferRequest request,
        CancellationToken cancellationToken)
        => Active.RedeemOfferAsync(request, cancellationToken);

    public Task<ZapsCancelRedemptionResult> CancelRedemptionAsync(
        ZapsCancelRedemptionRequest request,
        CancellationToken cancellationToken)
    {
        if (options.Value.Mode == ZapsClientMode.Real)
        {
            logger.LogWarning(
                "Zaps mode is Real, but CancelRedemption has no confirmed real endpoint; " +
                "forcing the Mock implementation for redemption {RedemptionId}.",
                request.RedemptionId);
        }

        return mockClient.CancelRedemptionAsync(request, cancellationToken);
    }
}
