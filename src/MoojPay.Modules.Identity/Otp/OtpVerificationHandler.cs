namespace MoojPay.Modules.Identity.Otp;

/// <summary>
/// OTP verification handler, extended for marketplace checkout with a failed-attempt counter
/// scoped to the checkout session, escalating to support after 3 failures
/// (acr_01a07bf5813f7c73a4edb49f5a339139). This greenfield repository has no pre-existing OTP
/// handler to extend; this is the net-new handler that carries that extension.
/// </summary>
public sealed class OtpVerificationHandler(IOtpAttemptTracker tracker)
{
    public async Task<OtpVerificationOutcome> VerifyAsync(
        string checkoutSessionId,
        string submittedOtp,
        string expectedOtp,
        CancellationToken cancellationToken)
    {
        if (string.Equals(submittedOtp, expectedOtp, StringComparison.Ordinal))
        {
            await tracker.ResetAsync(checkoutSessionId, cancellationToken).ConfigureAwait(false);
            return OtpVerificationOutcome.Success();
        }

        var result = await tracker.RecordFailureAsync(checkoutSessionId, cancellationToken).ConfigureAwait(false);

        return result.EscalatedToSupport
            ? OtpVerificationOutcome.Escalate(result.FailureCount)
            : OtpVerificationOutcome.Failed(result.FailureCount);
    }
}
