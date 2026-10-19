using Microsoft.EntityFrameworkCore;
using Pedidos.Application.Interfaces.Demo;
using Pedidos.Application.Interfaces.Pedidos;
using Pedidos.Application.Pedidos.Commands.CrearPedido;
using Pedidos.Application.Pedidos.Queries.ListarPedidos;
using Pedidos.Application.Pedidos.Queries.VentasPorDia;
using Pedidos.Application.Interfaces.Productos;
using Pedidos.Domain.Common;
using Pedidos.Domain.Pedidos;
using Pedidos.Infrastructure.Persistence;

namespace Pedidos.Infrastructure.Demo;

// Cada método de esta clase está MAL a propósito (Clase 4): sirve para compararlo con la versión buena
// (PedidosConsultas, PedidoRepository) mirando el SQL y los tiempos en la consola.
// S2077 / EF1002 / EF1003 (SQL concatenado) y S3267 (consulta dentro de un bucle) se desactivan solo aquí.
#pragma warning disable S2077, S3267, EF1002, EF1003

/// <summary>Versiones "malas" de las prácticas de la Clase 4. No la usa ningún caso de uso real.</summary>
public sealed class DemoMalasPracticas(AppDbContext context, IPedidoRepository repositorioPedidos, ICatalogoProductos catalogo) : IDemoMalasPracticas
{
    public async Task<IReadOnlyList<Pedido>> PedidosDelClienteComoEntidadAsync(Guid clienteId) =>
        // MAL: devuelve la entidad completa, con todas las columnas, las líneas y tracking
        await context.Pedidos
            .Include(p => p.Lineas)
            .Where(p => p.ClienteId == clienteId)
            .ToListAsync();

    public async Task<IReadOnlyList<PedidoConLineasDto>> PedidosConLineasN1Async(int cantidad)
    {
        var pedidos = await context.Pedidos
            .OrderBy(p => p.CreadoEn).ThenBy(p => p.Id)
            .Take(cantidad)
            .ToListAsync();                                                   // 1 consulta

        var resultado = new List<PedidoConLineasDto>();
        foreach (var pedido in pedidos)
        {
            // MAL: una consulta más por cada pedido (N+1)
            var lineas = await context.LineasPedido
                .Where(l => EF.Property<Guid>(l, "PedidoId") == pedido.Id)
                .ToListAsync();

            resultado.Add(new PedidoConLineasDto(
                pedido.Id,
                lineas.Select(l => new LineaResumenDto(l.ProductoId, l.Cantidad)).ToList()));
        }

        return resultado;
    }

    public async Task<IReadOnlyList<VentaPorDiaDto>> VentasPorDiaEnMemoriaAsync()
    {
        // MAL: trae todos los pedidos con sus líneas y agrupa en memoria lo que la base haría con un GROUP BY
        var pedidos = await context.Pedidos.Include(p => p.Lineas).ToListAsync();

        return pedidos
            .Where(p => p.Estado == EstadoPedido.Confirmado)
            .GroupBy(p => (p.CreadoEn.Date, p.Moneda))
            .Select(g => new VentaPorDiaDto(
                g.Key.Date,
                g.Key.Moneda,
                g.Sum(p => p.Lineas.Sum(l => l.PrecioUnitario.Monto * l.Cantidad))))
            .OrderBy(v => v.Dia)
            .ToList();
    }

    public async Task<Guid> CrearPedidoYDescontarStockSinTransaccionAsync(CrearPedidoCommand comando, DateTime creadoEn)
    {
        // Paso 1: guarda el pedido con sus líneas (queda confirmado en la base)
        var lineas = await catalogo.ResolverLineasAsync(comando, CancellationToken.None);
        var pedido = await repositorioPedidos.CrearAsync(
            comando.ClienteId, comando.ToEmail(), comando.ToDireccion(), comando.Moneda,
            creadoEn, lineas);

        // Paso 2: descuenta el stock línea por línea en el Catálogo. MAL: sin compensación, si una línea falla
        // el pedido y el stock de las líneas anteriores ya quedaron guardados.
        foreach (var linea in lineas)
            await catalogo.DescontarStockAsync(linea.Categoria, linea.ProductoId, linea.Cantidad);

        return pedido.Id;
    }
}

#pragma warning restore S2077, S3267, EF1002, EF1003
