namespace MoojPay.Modules.Marketplace.Contracts;

// Shapes mirror contract ctr_01a07c55ed437c568e493b9a8e942d18 (Mobile App <-> Backend API —
// Marketplace) exactly: envelope { data, success }, RFC 7807 problem details on error.

public sealed record MarketplaceOfferDto(
    string Id,
    string Title,
    decimal Price,
    string Currency,
    string? Category = null,
    string? ImageUrl = null,
    string? StartDate = null,
    string? EndDate = null,
    string? Description = null,
    string? MerchantName = null,
    int? QuantityAvailable = null,
    string? TermsAndConditions = null,
    string? RedemptionMethodType = null);

public sealed record MarketplaceOffersPageDto(
    IReadOnlyList<MarketplaceOfferDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

public sealed record RedeemCartItemRequest(string OfferId, int Quantity);

public sealed record RedeemCartRequest(
    IReadOnlyList<RedeemCartItemRequest> Items,
    string IdempotencyKey,
    string? PaymentReference);

public sealed record MarketplaceOrderItemDto(string OfferId, int Quantity, decimal UnitPrice, string? Title);

public sealed record MarketplaceOrderDto(
    string Id,
    string Status,
    IReadOnlyList<MarketplaceOrderItemDto> Items,
    decimal TotalAmount,
    string Currency,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    string? ZapsPurchaseReference);

public static class ApiEnvelope
{
    public sealed record Ok<T>(T Data, bool Success = true);
}
