using Npgsql;

namespace MoojPay.Modules.Identity.Otp;

/// <inheritdoc cref="IOtpAttemptTracker" />
public sealed class OtpAttemptTracker(NpgsqlDataSource dataSource) : IOtpAttemptTracker
{
    public const int MaxAttemptsBeforeEscalation = 3;

    public async Task<OtpAttemptResult> RecordFailureAsync(string checkoutSessionId, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO otp_checkout_attempts (checkout_session_id, failure_count, escalated_to_support, updated_at)
            VALUES (@session, 1, false, now())
            ON CONFLICT (checkout_session_id)
            DO UPDATE SET
                failure_count = otp_checkout_attempts.failure_count + 1,
                updated_at = now()
            RETURNING failure_count;
            """,
            connection);
        command.Parameters.AddWithValue("session", checkoutSessionId);

        var failureCount = (int)(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false))!;
        var escalated = failureCount >= MaxAttemptsBeforeEscalation;

        if (escalated)
        {
            await using var markEscalated = new NpgsqlCommand(
                "UPDATE otp_checkout_attempts SET escalated_to_support = true WHERE checkout_session_id = @session",
                connection);
            markEscalated.Parameters.AddWithValue("session", checkoutSessionId);
            await markEscalated.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        return new OtpAttemptResult(failureCount, escalated);
    }

    public async Task ResetAsync(string checkoutSessionId, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var command = new NpgsqlCommand(
            "DELETE FROM otp_checkout_attempts WHERE checkout_session_id = @session",
            connection);
        command.Parameters.AddWithValue("session", checkoutSessionId);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
