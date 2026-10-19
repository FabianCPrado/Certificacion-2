using Pedidos.Application.Pedidos.Commands.CrearPedido;
using Pedidos.Application.Pedidos.Queries.ListarPedidos;
using Pedidos.Application.Pedidos.Queries.VentasPorDia;
using Pedidos.Application.Productos.Queries.ObtenerProducto;
using Pedidos.Domain.Pedidos;

namespace Pedidos.Application.Interfaces.Demo;

/// <summary>
/// Solo para la Clase 4: las versiones "malas" de cada práctica, para compararlas con las buenas
/// (IProductoConsultas, IPedidosConsultas, IProductoRepository). Infrastructure las implementa a propósito
/// mal: no se usa en ningún caso de uso real y solo se expone en Development.
/// </summary>
public interface IDemoMalasPracticas
{
    /// <summary>ToList demasiado temprano: trae toda la tabla y filtra, ordena y pagina en memoria.</summary>
    Task<IReadOnlyList<ProductoDto>> ProductosFiltradosEnMemoriaAsync(decimal precioMinimo, int cantidad);

    /// <summary>Devuelve la entidad completa con sus líneas y con tracking.</summary>
    Task<IReadOnlyList<Pedido>> PedidosDelClienteComoEntidadAsync(Guid clienteId);

    /// <summary>Problema N+1: una consulta para los pedidos y otra por cada pedido.</summary>
    Task<IReadOnlyList<PedidoConLineasDto>> PedidosConLineasN1Async(int cantidad);

    /// <summary>Vulnerable a SQL injection: concatena el valor dentro del SQL.</summary>
    Task<IReadOnlyList<ProductoDto>> ProductosPorNombreInyectableAsync(string nombre);

    /// <summary>Agrupa en memoria, trayendo todos los pedidos con sus líneas.</summary>
    Task<IReadOnlyList<VentaPorDiaDto>> VentasPorDiaEnMemoriaAsync();

    /// <summary>
    /// Sin transacción: guarda el pedido y después descuenta el stock línea por línea. Si falla una línea,
    /// el pedido y el stock de las anteriores quedan guardados a medias.
    /// </summary>
    Task<Guid> CrearPedidoYDescontarStockSinTransaccionAsync(CrearPedidoCommand comando, DateTime creadoEn);
}
