using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MoojPay.Modules.Marketplace.Firebase;

/// <summary>
/// The <see cref="IFirebaseClient"/> actually registered in DI. Always routes to the Mock
/// implementation regardless of <see cref="FirebaseOptions.Mode"/>: no Firebase project is
/// provisioned for MoojPay yet, and the BRD/TRD provider-count conflict
/// (obs_01a07af31d097d78abca97763cd0fb77) is still open. Flip this once both are resolved.
/// </summary>
public sealed class FirebaseClientRouter(
    [FromKeyedServices("firebase-mock")] IFirebaseClient mockClient,
    IOptions<FirebaseOptions> options,
    ILogger<FirebaseClientRouter> logger) : IFirebaseClient
{
    public const string MockKey = "firebase-mock";

    public Task<FirebasePushResult> SendPushNotificationAsync(
        FirebasePushRequest request,
        CancellationToken cancellationToken)
    {
        if (options.Value.Mode == FirebaseClientMode.Real)
        {
            logger.LogWarning(
                "Firebase mode is Real, but no Firebase project is confirmed/provisioned for MoojPay yet; " +
                "forcing the Mock implementation for device {DeviceToken}.",
                request.DeviceToken);
        }

        return mockClient.SendPushNotificationAsync(request, cancellationToken);
    }
}
