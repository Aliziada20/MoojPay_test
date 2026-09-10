using Microsoft.Extensions.Logging;

namespace MoojPay.Modules.Marketplace.Firebase;

/// <summary>
/// Records the push send without contacting a real Firebase project. This is the only
/// <see cref="IFirebaseClient"/> implementation reachable at runtime today; see
/// <see cref="FirebaseClientRouter"/>.
/// </summary>
public sealed class MockFirebaseClient(ILogger<MockFirebaseClient> logger) : IFirebaseClient
{
    public Task<FirebasePushResult> SendPushNotificationAsync(
        FirebasePushRequest request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Mock Firebase push to device={DeviceToken} title={Title}",
            request.DeviceToken,
            request.Notification.Title);

        return Task.FromResult(new FirebasePushResult(Sent: true, MessageName: $"mock-message-{Guid.NewGuid():N}", Error: null));
    }
}
