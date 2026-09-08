using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MoojPay.Modules.Marketplace.Contracts;
using MoojPay.Modules.Marketplace.Services;

namespace MoojPay.Modules.Marketplace.Endpoints;

/// <summary>
/// Maps the three accepted marketplace operations from contract ctr_01a07c55ed437c568e493b9a8e942d18:
/// listMarketplaceOffers, redeemMarketplaceOffers, getMarketplaceOrder. Every route requires the
/// same Keycloak JWT bearer auth as the rest of the API (BuildSpec security note).
/// </summary>
public static class MarketplaceEndpoints
{
    public static IEndpointRouteBuilder MapMarketplaceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/marketplace").RequireAuthorization();

        group.MapGet("/offers", ListOffersAsync).WithName("listMarketplaceOffers");
        group.MapPost("/offers/{id}/redeem", RedeemOffersAsync).WithName("redeemMarketplaceOffers");
        group.MapGet("/orders/{id}", GetOrderAsync).WithName("getMarketplaceOrder");

        return endpoints;
    }

    private static async Task<IResult> ListOffersAsync(
        MarketplaceOrderService service,
        int? pageNumber,
        int? pageSize,
        int? categoryId,
        CancellationToken cancellationToken)
    {
        var page = await service.ListOffersAsync(pageNumber ?? 0, pageSize ?? 20, categoryId, cancellationToken)
            .ConfigureAwait(false);

        return Results.Ok(new ApiEnvelope.Ok<MarketplaceOffersPageDto>(page));
    }

    private static async Task<IResult> RedeemOffersAsync(
        string id,
        RedeemCartRequest request,
        MarketplaceOrderService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var customerId = httpContext.User.FindFirst("sub")?.Value
            ?? httpContext.User.Identity?.Name
            ?? throw new InvalidOperationException("Authenticated request is missing a subject claim.");

        var deviceToken = httpContext.Request.Headers["X-Device-Token"].FirstOrDefault();

        var outcome = await service.RedeemCartAsync(id, request, customerId, deviceToken, cancellationToken)
            .ConfigureAwait(false);

        return outcome.Kind switch
        {
            RedeemOutcomeKind.Accepted => Results.Json(
                new ApiEnvelope.Ok<MarketplaceOrderDto>(outcome.Order!),
                statusCode: StatusCodes.Status202Accepted),
            RedeemOutcomeKind.Validation => Results.Problem(
                title: "validation_error",
                detail: outcome.Message,
                statusCode: StatusCodes.Status400BadRequest),
            RedeemOutcomeKind.Conflict => Results.Problem(
                title: "offer_unavailable",
                detail: outcome.Message,
                statusCode: StatusCodes.Status409Conflict),
            _ => Results.Problem(
                title: "upstream_zaps_error",
                detail: outcome.Message,
                statusCode: StatusCodes.Status502BadGateway)
        };
    }

    private static async Task<IResult> GetOrderAsync(
        string id,
        MarketplaceOrderService service,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(id, out var orderId))
        {
            return Results.Problem(title: "not_found", statusCode: StatusCodes.Status404NotFound);
        }

        var order = await service.GetOrderAsync(orderId, cancellationToken).ConfigureAwait(false);
        return order is null
            ? Results.Problem(title: "not_found", statusCode: StatusCodes.Status404NotFound)
            : Results.Ok(new ApiEnvelope.Ok<MarketplaceOrderDto>(order));
    }
}
