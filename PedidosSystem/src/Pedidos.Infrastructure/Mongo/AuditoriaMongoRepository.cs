using Pedidos.Application.Interfaces.Auditoria;

namespace Pedidos.Infrastructure.Mongo;

/// <summary>Guarda la auditoría en MongoDB. Reemplaza al INSERT en SQL; Application sigue usando <see cref="IAuditoriaRepository"/>.</summary>
public sealed class AuditoriaMongoRepository(MongoAuditoriaColeccion mongo, TimeProvider tiempo) : IAuditoriaRepository
{
    private const int MaxRuta = 300;
    private const int MaxUsuario = 100;
    private const int MaxTraceId = 100;

    public Task RegistrarAsync(NuevaAuditoria auditoria, CancellationToken cancellationToken = default)
    {
        var documento = new AuditoriaDocumento
        {
            Id = Guid.NewGuid(),
            FechaUtc = tiempo.GetUtcNow().UtcDateTime,
            Metodo = auditoria.Metodo,
            Ruta = Recortar(auditoria.Ruta, MaxRuta),
            EstadoHttp = auditoria.EstadoHttp,
            DuracionMs = auditoria.DuracionMs,
            Usuario = Recortar(auditoria.Usuario, MaxUsuario),
            TraceId = Recortar(auditoria.TraceId, MaxTraceId),
            Request = auditoria.Request
        };

        return mongo.Coleccion.InsertOneAsync(documento, cancellationToken: cancellationToken);
    }

    private static string Recortar(string valor, int maximo) =>
        valor.Length <= maximo ? valor : valor[..maximo];
}
