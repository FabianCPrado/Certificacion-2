using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Pedidos.Infrastructure.Mongo;

/// <summary>Cómo se guarda un registro de auditoría en MongoDB (colección "auditoria"). Es un detalle de Infrastructure.</summary>
public sealed class AuditoriaDocumento
{
    [BsonId]
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid Id { get; set; }

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime FechaUtc { get; set; }

    public string Metodo { get; set; } = null!;

    public string Ruta { get; set; } = null!;

    public int EstadoHttp { get; set; }

    public int DuracionMs { get; set; }

    public string Usuario { get; set; } = null!;

    public string TraceId { get; set; } = null!;

    public string? Request { get; set; }
}
