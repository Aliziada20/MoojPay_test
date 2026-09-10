using MoojPay.Domain.Enums;

namespace MoojPay.Domain.Entities;

/// <summary>
/// MoojPay's own marketplace purchase/order record. Net-new persistence: not present in the
/// TRD's documented data model (TRD §9), added per SA review on
/// cmp_01a07bffc5957c1e8b1256778ce9da94 to satisfy the accepted "Post-purchase order status and
/// confirmation" requirement (req_01a07bf5813f744da27e0e86da5e61b8).
///
/// PCI-DSS boundary note: this entity never stores a card number, PIN, or wallet credential.
/// <see cref="ZapsPurchaseReference"/> and the mobile app's Bank SDK payment reference (accepted
/// by the redeem endpoint, not persisted verbatim on this entity) are opaque reference strings
/// only.
/// </summary>
public sealed class MarketplaceOrder
{
    public required Guid Id { get; init; }

    public required MarketplaceOrderStatus Status { get; set; }

    public required IReadOnlyList<MarketplaceOrderItem> Items { get; set; }

    public required decimal TotalAmount { get; set; }

    public required string Currency { get; set; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Zaps PurchaseId/RedemptionId reference, reference-only per TRD §9 data principles.</summary>
    public string? ZapsPurchaseReference { get; set; }

    /// <summary>
    /// Idempotency key from the redeem request. Zaps's confirmed RedeemOffer endpoint carries no
    /// dedup key of its own (contract ctr_01a07c565b127fd4a81217917541f8b9 notes
    /// <c>x-idempotency: false</c> and "not documented as retry-safe"), so MoojPay enforces
    /// dedup at this layer: a retried checkout submission with the same key returns the
    /// already-created order instead of redeeming twice.
    /// </summary>
    public required string IdempotencyKey { get; init; }
}
