using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace MoojPay.Modules.Marketplace.Firebase;

/// <summary>
/// Live Firebase Cloud Messaging HTTP v1 client, grounded in FCM's real public API shape
/// (contract ctr_01a07da0076b781291953e2ca77134af). Never invoked by <see cref="FirebaseClientRouter"/>
/// today: no Firebase project/service-account is confirmed or provisioned for MoojPay yet, and
/// the underlying BRD/TRD provider-count conflict (obs_01a07af31d097d78abca97763cd0fb77) is still
/// open. Kept for when both are resolved.
/// </summary>
public sealed class RealFirebaseClient(HttpClient httpClient, IOptions<FirebaseOptions> options) : IFirebaseClient
{
    public async Task<FirebasePushResult> SendPushNotificationAsync(
        FirebasePushRequest request,
        CancellationToken cancellationToken)
    {
        var projectId = options.Value.ProjectId;
        var body = new
        {
            message = new
            {
                token = request.DeviceToken,
                notification = new { title = request.Notification.Title, body = request.Notification.Body },
                data = request.Data
            }
        };

        var response = await httpClient
            .PostAsJsonAsync($"v1/projects/{projectId}/messages:send", body, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return new FirebasePushResult(Sent: false, MessageName: null, Error: error);
        }

        var payload = await response.Content.ReadFromJsonAsync<FcmSendResponse>(cancellationToken)
            .ConfigureAwait(false);

        return new FirebasePushResult(Sent: true, MessageName: payload?.Name, Error: null);
    }

    private sealed record FcmSendResponse(string? Name);
}
