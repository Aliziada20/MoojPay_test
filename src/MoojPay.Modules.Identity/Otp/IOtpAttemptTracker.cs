namespace MoojPay.Modules.Identity.Otp;

/// <summary>
/// Tracks failed OTP verification attempts scoped to one checkout session, persisted in
/// PostgreSQL so the count survives process restarts (table <c>otp_checkout_attempts</c>,
/// created by <c>0002_create_otp_checkout_attempts.sql</c>).
/// </summary>
public interface IOtpAttemptTracker
{
    Task<OtpAttemptResult> RecordFailureAsync(string checkoutSessionId, CancellationToken cancellationToken);

    Task ResetAsync(string checkoutSessionId, CancellationToken cancellationToken);
}
