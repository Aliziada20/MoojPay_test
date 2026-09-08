using Microsoft.AspNetCore.Authentication.JwtBearer;
using MoojPay.Modules.Identity;
using MoojPay.Modules.Marketplace;
using MoojPay.Modules.Marketplace.Endpoints;
using MoojPay.Modules.Marketplace.Persistence.SqlMigrations;

var builder = WebApplication.CreateBuilder(args);

// Keycloak-issued JWT bearer auth, consistent with the rest of the API (BuildSpec security note),
// applied to every marketplace route via MapMarketplaceEndpoints().RequireAuthorization().
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Keycloak:Authority"];
        options.Audience = builder.Configuration["Keycloak:Audience"];
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    });
builder.Services.AddAuthorization();

builder.Services.AddMarketplaceModule(builder.Configuration);
builder.Services.AddIdentityModule(builder.Configuration);

builder.Services.AddHealthChecks();

var app = builder.Build();

// Additive-only raw SQL migrations applied at startup (see SqlMigrationRunner); no EF Core
// generated-migration workflow is used, per the accepted BuildSpec's stack decision.
using (var scope = app.Services.CreateScope())
{
    var migrationRunner = scope.ServiceProvider.GetRequiredService<SqlMigrationRunner>();
    await migrationRunner.ApplyAsync();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready");

app.MapMarketplaceEndpoints();

app.Run();

/// <summary>Exposed for WebApplicationFactory-based integration tests once they are authorized.</summary>
public partial class Program;
