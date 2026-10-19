using Pedidos.Application.Auditoria.Queries.ListarAuditoria;
using Pedidos.Application.Common;

namespace Pedidos.Application.Interfaces.Auditoria;

/// <summary>Consultas de solo lectura de la auditoría: proyectan directo al DTO.</summary>
public interface IAuditoriaConsultas
{
    Task<PagedResult<AuditoriaDto>> ListarAsync(
        int pagina, int tamanio, string? ruta, DateTime? desde, DateTime? hasta,
        CancellationToken cancellationToken = default);
}
