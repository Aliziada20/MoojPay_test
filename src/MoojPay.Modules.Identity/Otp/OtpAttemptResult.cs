namespace MoojPay.Modules.Identity.Otp;

public sealed record OtpAttemptResult(int FailureCount, bool EscalatedToSupport);

public enum OtpVerificationStatus
{
    Success,
    Failed,
    EscalatedToSupport
}

public sealed record OtpVerificationOutcome(OtpVerificationStatus Status, int FailureCount)
{
    public static OtpVerificationOutcome Success() => new(OtpVerificationStatus.Success, 0);

    public static OtpVerificationOutcome Failed(int failureCount) => new(OtpVerificationStatus.Failed, failureCount);

    public static OtpVerificationOutcome Escalate(int failureCount) => new(OtpVerificationStatus.EscalatedToSupport, failureCount);
}
