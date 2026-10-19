namespace Pedidos.Application.Interfaces.Auditoria;

/// <summary>Datos de una operación a auditar. Solo transporta datos.</summary>
public sealed record NuevaAuditoria(
    string Metodo,
    string Ruta,
    int EstadoHttp,
    int DuracionMs,
    string Usuario,
    string TraceId,
    string? Request);

/// <summary>Guarda el registro de auditoría. La define Application e Infrastructure la implementa.</summary>
public interface IAuditoriaRepository
{
    Task RegistrarAsync(NuevaAuditoria auditoria, CancellationToken cancellationToken = default);
}
