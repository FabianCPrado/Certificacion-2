using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Pedidos.Infrastructure.Mongo;

/// <summary>
/// Acceso a la colección "auditoria". Es un singleton: el cliente de MongoDB maneja su propio pool de conexiones
/// y está pensado para compartirse en toda la aplicación.
/// </summary>
public sealed class MongoAuditoriaColeccion
{
    private const string NombreColeccion = "auditoria";
    private const string BaseDatosPorDefecto = "PedidosDb";

    public IMongoCollection<AuditoriaDocumento> Coleccion { get; }

    public MongoAuditoriaColeccion(string cadenaConexion, ILogger<MongoAuditoriaColeccion> logger)
    {
        var url = MongoUrl.Create(cadenaConexion);
        var configuracion = MongoClientSettings.FromUrl(url);

        // Si MongoDB está caído, falla en 2 segundos y no a los 30 por defecto: la auditoría corre en cada POST/PUT/DELETE
        configuracion.ServerSelectionTimeout = TimeSpan.FromSeconds(2);

        var baseDatos = new MongoClient(configuracion).GetDatabase(url.DatabaseName ?? BaseDatosPorDefecto);
        Coleccion = baseDatos.GetCollection<AuditoriaDocumento>(NombreColeccion);

        CrearIndices(logger);
    }

    // Equivalen a los índices de la tabla SQL (FechaUtc y Ruta). CreateOne es idempotente: si ya existen, no hace nada.
    private void CrearIndices(ILogger logger)
    {
        try
        {
            Coleccion.Indexes.CreateMany(
            [
                new CreateIndexModel<AuditoriaDocumento>(Builders<AuditoriaDocumento>.IndexKeys.Descending(a => a.FechaUtc)),
                new CreateIndexModel<AuditoriaDocumento>(Builders<AuditoriaDocumento>.IndexKeys.Ascending(a => a.Ruta))
            ]);
        }
        catch (Exception ex)
        {
            // La app debe arrancar aunque MongoDB todavía no esté disponible
            logger.LogWarning(ex, "No se pudieron crear los índices de la colección de auditoría en MongoDB.");
        }
    }
}
