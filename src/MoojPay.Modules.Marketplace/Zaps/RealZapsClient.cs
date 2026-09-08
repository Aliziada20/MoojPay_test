using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MoojPay.Modules.Marketplace.Zaps;

/// <summary>
/// Live Zaps HTTP client. Only ListOffers and RedeemOffer are confirmed endpoints (Zaps
/// Integration reference src_01a07acb3a8f7d8fb3848994f62b9b9d). CancelRedemption is implemented
/// against the placeholder/hypothetical shape for completeness, but <see cref="ZapsClientRouter"/>
/// never routes a live call to it: it is not a confirmed Zaps API.
/// </summary>
public sealed class RealZapsClient(HttpClient httpClient, IOptions<ZapsOptions> options, ILogger<RealZapsClient> logger) : IZapsClient
{
    private readonly ZapsOptions _options = options.Value;

    public async Task<IReadOnlyList<ZapsOffer>> ListOffersAsync(
        int pageNumber,
        int pageSize,
        int? categoryId,
        CancellationToken cancellationToken)
    {
        var query = new Dictionary<string, string?>
        {
            ["Latitude"] = _options.DefaultLatitude.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["Longitude"] = _options.DefaultLongitude.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["Distance"] = _options.SearchRadius.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["PageNumber"] = pageNumber.ToString(),
            ["PageSize"] = (pageSize > 0 ? pageSize : _options.PageSize).ToString(),
            ["clientId"] = _options.ClientId,
            ["countryCode"] = _options.DefaultCountryCode,
            ["distanceUnit"] = _options.DistanceUnit
        };

        var uri = QueryHelpers.AddQueryString("customers/fetchNearByCoupon", query);
        var response = await httpClient.GetFromJsonAsync<ZapsListOffersResponse>(uri, cancellationToken)
            .ConfigureAwait(false);

        return response?.Items is null
            ? []
            : response.Items
                .Select(o => new ZapsOffer(
                    o.Id,
                    o.Title,
                    o.Price,
                    string.IsNullOrWhiteSpace(o.Currency) ? "SAR" : o.Currency,
                    o.Category,
                    o.ImageUrl,
                    o.StartDate,
                    o.EndDate,
                    o.MerchantName,
                    o.Quantity,
                    o.TermsAndConditions,
                    o.RedemptionMethodType))
                .ToList();
    }

    public async Task<ZapsRedemptionResult?> RedeemOfferAsync(
        ZapsRedeemOfferRequest request,
        CancellationToken cancellationToken)
    {
        var body = new
        {
            request.StoreId,
            request.CouponId,
            request.StorePin,
            request.CouponCode,
            request.RedemptionType,
            request.UniqueIdentifier
        };

        var response = await httpClient.PostAsJsonAsync("customers/redeemCoupon", body, cancellationToken)
            .ConfigureAwait(false);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            logger.LogWarning("Zaps RedeemOffer returned 404 for coupon {CouponCode}", request.CouponCode);
            return null;
        }

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ZapsRedeemResponse>(cancellationToken)
            .ConfigureAwait(false);

        return payload is null ? null : new ZapsRedemptionResult(payload.Status, payload.RedemptionId);
    }

    public Task<ZapsCancelRedemptionResult> CancelRedemptionAsync(
        ZapsCancelRedemptionRequest request,
        CancellationToken cancellationToken)
    {
        // Not a confirmed Zaps API and never invoked by ZapsClientRouter. Kept for documentation
        // and to allow direct testing against the hypothetical shape once Zaps confirms it.
        throw new NotSupportedException(
            "Zaps has no confirmed cancel/refund endpoint. CancelRedemption must run Mock-only; " +
            "this method must not be called against the real Zaps API.");
    }

    private sealed record ZapsListOffersResponse(IReadOnlyList<ZapsWireOffer> Items);

    private sealed record ZapsWireOffer(
        string Id,
        string Title,
        decimal Price,
        string? Currency,
        string? Category,
        string? ImageUrl,
        string? StartDate,
        string? EndDate,
        string? MerchantName,
        int Quantity,
        string? TermsAndConditions,
        string? RedemptionMethodType);

    private sealed record ZapsRedeemResponse(string? Status, string? RedemptionId);
}

internal static class QueryHelpers
{
    public static string AddQueryString(string path, IDictionary<string, string?> query)
    {
        var parts = query
            .Where(kv => !string.IsNullOrEmpty(kv.Value))
            .Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value!)}");
        var queryString = string.Join('&', parts);
        return queryString.Length == 0 ? path : $"{path}?{queryString}";
    }
}
