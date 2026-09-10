namespace MoojPay.Modules.Marketplace.Firebase;

public enum FirebaseClientMode
{
    Mock,
    Real
}

/// <summary>
/// Configuration for the Firebase push integration. <see cref="Mode"/> defaults to
/// <see cref="FirebaseClientMode.Mock"/>. Regardless of configuration, every send is routed to
/// the Mock implementation by <see cref="FirebaseClientRouter"/> until a real Firebase project is
/// confirmed and provisioned for MoojPay.
/// </summary>
public sealed class FirebaseOptions
{
    public const string SectionName = "Firebase";

    public FirebaseClientMode Mode { get; set; } = FirebaseClientMode.Mock;

    public string ProjectId { get; set; } = string.Empty;
}
