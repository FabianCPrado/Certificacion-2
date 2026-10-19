using Microsoft.EntityFrameworkCore;
using Pedidos.Application.Common;
using Pedidos.Application.Interfaces.Pedidos;
using Pedidos.Application.Pedidos.Queries.ListarPedidos;
using Pedidos.Application.Pedidos.Queries.ObtenerPedido;
using Pedidos.Infrastructure.Persistence;

namespace Pedidos.Infrastructure.Queries.Pedidos;

/// <summary>Consultas de lectura con EF Core: proyecciones al DTO, sin tracking. El reporte con Dapper está en <see cref="PedidosReportes"/>.</summary>
public sealed class PedidosConsultas(AppDbContext context) : IPedidosConsultas
{
    public Task<PedidoDto?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        // Proyección al DTO: el total y los subtotales se calculan en la base; sin entidades ni tracking
        context.Pedidos.AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new PedidoDto(
                p.Id,
                p.ClienteId,
                p.EmailCliente.Valor,
                p.Estado.ToString(),
                p.Lineas.Sum(l => l.PrecioUnitario.Monto * l.Cantidad),
                p.Moneda,
                p.CreadoEn,
                p.Lineas.Select(l => new LineaPedidoDto(
                    l.ProductoId,
                    l.NombreProducto,
                    l.PrecioUnitario.Monto,
                    l.Cantidad,
                    l.PrecioUnitario.Monto * l.Cantidad)).ToList()))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<PagedResult<PedidoResumenDto>> ListarAsync(
        int pagina, int tamanio, Guid? clienteId, CancellationToken cancellationToken = default)
    {
        var consulta = context.Pedidos.AsNoTracking();

        if (clienteId is not null)
            consulta = consulta.Where(p => p.ClienteId == clienteId);

        var total = await consulta.CountAsync(cancellationToken);

        // Proyección al DTO: el total se calcula en la base y no se cargan las líneas
        var items = await consulta
            .OrderByDescending(p => p.CreadoEn)
            .Skip((pagina - 1) * tamanio).Take(tamanio)
            .Select(p => new PedidoResumenDto(
                p.Id,
                p.ClienteId,
                p.Estado.ToString(),
                p.Lineas.Sum(l => l.PrecioUnitario.Monto * l.Cantidad),
                p.Moneda,
                p.CreadoEn))
            .ToListAsync(cancellationToken);

        return new PagedResult<PedidoResumenDto>(items, pagina, tamanio, total);
    }

    public async Task<IReadOnlyList<PedidoDto>> ListarPorFechasAsync(
        DateOnly desde, DateOnly hasta, int maximo, CancellationToken cancellationToken = default)
    {
        // Rango semiabierto [desde 00:00, día siguiente a hasta 00:00): el día "hasta" entra completo
        var inicio = desde.ToDateTime(TimeOnly.MinValue);
        var fin = hasta.AddDays(1).ToDateTime(TimeOnly.MinValue);

        return await context.Pedidos.AsNoTracking()
            .Where(p => p.CreadoEn >= inicio && p.CreadoEn < fin)
            .OrderByDescending(p => p.CreadoEn)
            .Take(maximo)
            .Select(p => new PedidoDto(
                p.Id,
                p.ClienteId,
                p.EmailCliente.Valor,
                p.Estado.ToString(),
                p.Lineas.Sum(l => l.PrecioUnitario.Monto * l.Cantidad),
                p.Moneda,
                p.CreadoEn,
                p.Lineas.Select(l => new LineaPedidoDto(
                    l.ProductoId,
                    l.NombreProducto,
                    l.PrecioUnitario.Monto,
                    l.Cantidad,
                    l.PrecioUnitario.Monto * l.Cantidad)).ToList()))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PedidoConLineasDto>> ListarConLineasAsync(
        int cantidad, CancellationToken cancellationToken = default) =>
        // Una sola consulta con las líneas anidadas: sin N+1
        await context.Pedidos.AsNoTracking()
            .OrderBy(p => p.CreadoEn).ThenBy(p => p.Id)
            .Take(cantidad)
            .Select(p => new PedidoConLineasDto(
                p.Id,
                p.Lineas.Select(l => new LineaResumenDto(l.ProductoId, l.Cantidad)).ToList()))
            .ToListAsync(cancellationToken);
}
