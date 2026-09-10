using MoojPay.Modules.Marketplace.Contracts;

namespace MoojPay.Modules.Marketplace.Services;

public enum RedeemOutcomeKind
{
    Accepted,
    Validation,
    Conflict,
    UpstreamError
}

public sealed record RedeemOutcome(RedeemOutcomeKind Kind, MarketplaceOrderDto? Order, string? Message)
{
    public static RedeemOutcome Accepted(MarketplaceOrderDto order) => new(RedeemOutcomeKind.Accepted, order, null);

    public static RedeemOutcome Validation(string message) => new(RedeemOutcomeKind.Validation, null, message);

    public static RedeemOutcome Conflict(string message) => new(RedeemOutcomeKind.Conflict, null, message);

    public static RedeemOutcome UpstreamError(string message) => new(RedeemOutcomeKind.UpstreamError, null, message);
}
