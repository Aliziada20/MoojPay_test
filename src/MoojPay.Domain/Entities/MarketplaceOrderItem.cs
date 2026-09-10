namespace MoojPay.Domain.Entities;

/// <summary>
/// One cart line inside a persisted <see cref="MarketplaceOrder"/>. Stored as a JSON array
/// (see <c>0001_create_marketplace_order.sql</c>) rather than a child relational table,
/// matching the contract's embedded <c>items[]</c> shape.
/// </summary>
public sealed record MarketplaceOrderItem(
    string OfferId,
    int Quantity,
    decimal UnitPrice,
    string? Title = null);
