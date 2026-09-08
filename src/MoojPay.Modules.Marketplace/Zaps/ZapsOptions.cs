namespace MoojPay.Modules.Marketplace.Zaps;

public enum ZapsClientMode
{
    Mock,
    Real
}

/// <summary>
/// Configuration for the Zaps integration. <see cref="Mode"/> defaults to <see cref="ZapsClientMode.Mock"/>
/// per the accepted BuildSpec. Note: regardless of <see cref="Mode"/>, CancelRedemption always
/// runs Mock-only (enforced by <see cref="ZapsClientRouter"/>) because Zaps has no confirmed real
/// cancel endpoint.
/// </summary>
public sealed class ZapsOptions
{
    public const string SectionName = "Zaps";

    public ZapsClientMode Mode { get; set; } = ZapsClientMode.Mock;

    public string BaseUrl { get; set; } = "https://api.zaps.example/";

    public string ApiKey { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;

    public double DefaultLatitude { get; set; }

    public double DefaultLongitude { get; set; }

    public double SearchRadius { get; set; } = 25;

    public int PageSize { get; set; } = 20;

    public string DefaultCountryCode { get; set; } = "SA";

    public string DistanceUnit { get; set; } = "KM";
}
