using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MoojPay.Modules.Identity.Otp;
using Npgsql;

namespace MoojPay.Modules.Identity;

public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("MoojPayDatabase")
            ?? throw new InvalidOperationException("Missing ConnectionStrings:MoojPayDatabase configuration.");

        // Reuses the same NpgsqlDataSource registration contract as the Marketplace module; each
        // module registers its own singleton against the same connection string, avoiding a
        // cross-module project reference for a single shared type.
        services.TryAddSingleton(_ => NpgsqlDataSource.Create(connectionString));
        services.AddSingleton<IOtpAttemptTracker, OtpAttemptTracker>();
        services.AddTransient<OtpVerificationHandler>();

        return services;
    }
}
