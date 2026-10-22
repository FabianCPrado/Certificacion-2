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
        var hasher = provider.GetRequiredService<IClientSecretHasher>();

        await ProvisionClientAsync(db, hasher, logger, configuration,
            "pedidos-service", "Pedidos service", "ClientCredentials:PedidosSecret", cancellationToken);
        await ProvisionClientAsync(db, hasher, logger, configuration,
            "asistente-service", "Asistente service", "ClientCredentials:AsistenteSecret", cancellationToken);
    }

    private static async Task ProvisionClientAsync(
        AuthenticationDbContext db,
        IClientSecretHasher hasher,
        ILogger logger,
        IConfiguration configuration,
        string clientId,
        string name,
        string secretKey,
        CancellationToken cancellationToken)
    {
        var secret = configuration[secretKey];
        if (string.IsNullOrWhiteSpace(secret))
        {
            logger.LogWarning("No se provisionó el cliente {ClientId}: configure {SecretKey} mediante User Secrets.", clientId, secretKey);
            return;
        }

        if (await db.ServiceClients.AnyAsync(c => c.ClientId == clientId, cancellationToken))
            return;

        var client = new ServiceClient(clientId, name, "pending");
        client.ReplaceSecretHash(hasher.Hash(client, secret));
        db.ServiceClients.Add(client);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Se aprovisionó el cliente de servicio {ClientId}.", clientId);
    }
}