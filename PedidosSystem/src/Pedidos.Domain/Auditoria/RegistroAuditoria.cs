using Pedidos.Domain.Common;

namespace Pedidos.Domain.Auditoria;

/// <summary>Una operación que modificó datos (POST, PUT o DELETE): solo datos.</summary>
public sealed class RegistroAuditoria() : Entity<Guid>(Guid.NewGuid())
{
    public DateTime FechaUtc { get; set; }

    public string Metodo { get; set; } = null!;

    public string Ruta { get; set; } = null!;

    public int EstadoHttp { get; set; }

    public int DuracionMs { get; set; }

    public string Usuario { get; set; } = null!;

    public string TraceId { get; set; } = null!;

    /// <summary>Cuerpo del request tal cual llegó (JSON, recortado). Nulo si la operación no tiene cuerpo.</summary>
    public string? Request { get; set; }
}
