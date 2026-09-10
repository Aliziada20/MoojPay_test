namespace MoojPay.Modules.Marketplace.Firebase;

public sealed record FirebaseNotification(string Title, string Body);

public sealed record FirebasePushRequest(
    string DeviceToken,
    FirebaseNotification Notification,
    IReadOnlyDictionary<string, string>? Data = null);

public sealed record FirebasePushResult(bool Sent, string? MessageName, string? Error);

/// <summary>
/// Backend API -&gt; Firebase boundary (contract ctr_01a07da0076b781291953e2ca77134af, provider
/// component cmp_01a07bff4dd474818d871f838ac0abe2). Provisional: no MoojPay-owned document
/// confirms this integration and no Firebase project is provisioned yet, so every caller reaches
/// this only through <see cref="FirebaseClientRouter"/>, which forces Mock regardless of
/// configuration.
/// </summary>
public interface IFirebaseClient
{
    Task<FirebasePushResult> SendPushNotificationAsync(
        FirebasePushRequest request,
        CancellationToken cancellationToken);
}
