namespace MoojPay.Domain.Enums;

/// <summary>
/// Lifecycle status of a persisted <c>MarketplaceOrder</c>, matching the accepted
/// Mobile App &lt;-&gt; Backend API marketplace contract (ctr_01a07c55ed437c568e493b9a8e942d18).
/// </summary>
public enum MarketplaceOrderStatus
{
    Pending,
    Processing,
    Fulfilled,
    Failed,
    Refunded,
    Cancelled
}
