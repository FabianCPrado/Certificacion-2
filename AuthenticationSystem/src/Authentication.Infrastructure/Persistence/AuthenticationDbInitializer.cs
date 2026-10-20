using Authentication.Application.Clients;
using Authentication.Domain.Clients;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Authentication.Infrastructure.Persistence;

public static class AuthenticationDbInitializer
{
    public static async Task InitializeAuthenticationDbAsync(
        this IServiceProvider services,
        IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        var db = provider.GetRequiredService<AuthenticationDbContext>();
        await db.Database.MigrateAsync(cancellationToken);

        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("AuthenticationDbInitializer");
        var secret = configuration["ClientCredentials:PedidosSecret"];
        if (string.IsNullOrWhiteSpace(secret))
        {
            logger.LogWarning("No se provisionó el cliente Pedidos: configure ClientCredentials:PedidosSecret mediante User Secrets.");
            return;
        }

        const string clientId = "pedidos-service";
        if (await db.ServiceClients.AnyAsync(c => c.ClientId == clientId, cancellationToken))
            return;

        var client = new ServiceClient(clientId, "Pedidos service", "pending");
        client.ReplaceSecretHash(provider.GetRequiredService<IClientSecretHasher>().Hash(client, secret));
        db.ServiceClients.Add(client);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Se aprovisionó el cliente de servicio {ClientId}.", clientId);
    }
}