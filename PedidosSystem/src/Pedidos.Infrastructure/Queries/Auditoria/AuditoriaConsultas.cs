using Microsoft.EntityFrameworkCore;
using Pedidos.Application.Auditoria.Queries.ListarAuditoria;
using Pedidos.Application.Common;
using Pedidos.Application.Interfaces.Auditoria;
using Pedidos.Infrastructure.Persistence;

namespace Pedidos.Infrastructure.Queries.Auditoria;

public sealed class AuditoriaConsultas(AppDbContext context) : IAuditoriaConsultas
{
    public async Task<PagedResult<AuditoriaDto>> ListarAsync(
        int pagina, int tamanio, string? ruta, DateTime? desde, DateTime? hasta,
        CancellationToken cancellationToken = default)
    {
        var consulta = context.Auditoria.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(ruta))
            consulta = consulta.Where(a => a.Ruta.StartsWith(ruta));

        if (desde is not null)
            consulta = consulta.Where(a => a.FechaUtc >= desde);

        if (hasta is not null)
            consulta = consulta.Where(a => a.FechaUtc <= hasta);

        var total = await consulta.CountAsync(cancellationToken);

        var items = await consulta
            .OrderByDescending(a => a.FechaUtc)
            .Skip((pagina - 1) * tamanio).Take(tamanio)
            .Select(a => new AuditoriaDto(
                a.Id, a.FechaUtc, a.Metodo, a.Ruta, a.EstadoHttp, a.DuracionMs, a.Usuario, a.TraceId, a.Request))
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditoriaDto>(items, pagina, tamanio, total);
    }
}
