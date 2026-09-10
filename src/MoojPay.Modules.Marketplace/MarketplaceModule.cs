using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MoojPay.Modules.Marketplace.Firebase;
using MoojPay.Modules.Marketplace.Persistence;
using MoojPay.Modules.Marketplace.Persistence.SqlMigrations;
using MoojPay.Modules.Marketplace.Services;
using MoojPay.Modules.Marketplace.Zaps;
using Npgsql;

namespace MoojPay.Modules.Marketplace;

public static class MarketplaceModule
{
    public static IServiceCollection AddMarketplaceModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("MoojPayDatabase")
            ?? throw new InvalidOperationException("Missing ConnectionStrings:MoojPayDatabase configuration.");

        services.TryAddSingleton(_ => NpgsqlDataSource.Create(connectionString));
        services.AddSingleton<SqlMigrationRunner>();

        services.AddDbContext<MarketplaceDbContext>(options => options.UseNpgsql(connectionString));

        services.Configure<ZapsOptions>(configuration.GetSection(ZapsOptions.SectionName));
        services.Configure<FirebaseOptions>(configuration.GetSection(FirebaseOptions.SectionName));

        services.AddKeyedTransient<IZapsClient, MockZapsClient>(ZapsClientRouter.MockKey);
        services.AddHttpClient<RealZapsClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ZapsOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            if (!string.IsNullOrEmpty(options.ApiKey))
            {
                client.DefaultRequestHeaders.Add("x-api-key", options.ApiKey);
            }
        });
        services.AddKeyedTransient<IZapsClient>(ZapsClientRouter.RealKey, (sp, _) => sp.GetRequiredService<RealZapsClient>());
        services.AddTransient<IZapsClient, ZapsClientRouter>();

        services.AddKeyedTransient<IFirebaseClient, MockFirebaseClient>(FirebaseClientRouter.MockKey);
        services.AddHttpClient<RealFirebaseClient>(client =>
        {
            client.BaseAddress = new Uri("https://fcm.googleapis.com/");
        });
        services.AddTransient<IFirebaseClient, FirebaseClientRouter>();

        services.AddScoped<MarketplaceOrderService>();

        return services;
    }
}
