using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Driver;
using Pedidos.Application.Auditoria.Queries.ListarAuditoria;
using Pedidos.Application.Common;
using Pedidos.Application.Interfaces.Auditoria;

namespace Pedidos.Infrastructure.Mongo;

/// <summary>Lee la auditoría desde MongoDB: filtra, ordena y pagina en la base y proyecta al DTO.</summary>
public sealed class AuditoriaMongoConsultas(MongoAuditoriaColeccion mongo) : IAuditoriaConsultas
{
    public async Task<PagedResult<AuditoriaDto>> ListarAsync(
        int pagina, int tamanio, string? ruta, DateTime? desde, DateTime? hasta,
        CancellationToken cancellationToken = default)
    {
        var filtro = Construir(ruta, desde, hasta);

        var total = await mongo.Coleccion.CountDocumentsAsync(filtro, cancellationToken: cancellationToken);

        var documentos = await mongo.Coleccion.Find(filtro)
            .SortByDescending(a => a.FechaUtc)
            .Skip((pagina - 1) * tamanio).Limit(tamanio)
            .ToListAsync(cancellationToken);

        var items = documentos
            .Select(a => new AuditoriaDto(
                a.Id, a.FechaUtc, a.Metodo, a.Ruta, a.EstadoHttp, a.DuracionMs, a.Usuario, a.TraceId, a.Request))
            .ToList();

        return new PagedResult<AuditoriaDto>(items, pagina, tamanio, (int)total);
    }

    private static FilterDefinition<AuditoriaDocumento> Construir(string? ruta, DateTime? desde, DateTime? hasta)
    {
        var f = Builders<AuditoriaDocumento>.Filter;
        var filtros = new List<FilterDefinition<AuditoriaDocumento>>();

        // "Empieza con": expresión regular anclada al inicio (así Mongo puede usar el índice de Ruta)
        if (!string.IsNullOrWhiteSpace(ruta))
            filtros.Add(f.Regex(a => a.Ruta, new BsonRegularExpression("^" + Regex.Escape(ruta))));

        if (desde is not null)
            filtros.Add(f.Gte(a => a.FechaUtc, desde.Value));

        if (hasta is not null)
            filtros.Add(f.Lte(a => a.FechaUtc, hasta.Value));

        return filtros.Count == 0 ? f.Empty : f.And(filtros);
    }
}
