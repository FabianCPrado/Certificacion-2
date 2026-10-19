using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Pedidos.Infrastructure.Cosmos;

public static class CosmosInicializador
{
    /// <summary>
    /// Solo en Development: crea la base, el contenedor del catálogo (SDK) y el de EF Core. Si el emulador no está
    /// corriendo, avisa y deja arrancar la aplicación igual.
    /// </summary>
    public static async Task InicializarCosmosAsync(this IServiceProvider servicios)
    {
        var log = servicios.GetRequiredService<ILoggerFactory>().CreateLogger("CosmosInicializador");
        using var limite = new CancellationTokenSource(TimeSpan.FromSeconds(20));

        try
        {
            var cliente = servicios.GetRequiredService<CosmosClient>();
            var baseDatos = await cliente.CreateDatabaseIfNotExistsAsync(CosmosOpciones.BaseDatos, cancellationToken: limite.Token);
            await baseDatos.Database.CreateContainerIfNotExistsAsync(
                new ContainerProperties(CosmosOpciones.ContenedorCatalogo, CosmosOpciones.ClaveParticionCatalogo),
                cancellationToken: limite.Token);

            // Contenedor "ProductosEf": EF Core no usa migraciones con Cosmos, usa EnsureCreated
            using var alcance = servicios.CreateScope();
            await alcance.ServiceProvider.GetRequiredService<CatalogoDbContext>().Database.EnsureCreatedAsync(limite.Token);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "No se pudo inicializar Cosmos DB (¿está corriendo el emulador? docker compose up -d). Las demos de Cosmos no van a responder.");
        }
    }
}
