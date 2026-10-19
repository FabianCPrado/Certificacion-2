using Pedidos.Application.Common;
using Pedidos.Application.Pedidos.Queries.ListarPedidos;
using Pedidos.Application.Pedidos.Queries.ObtenerPedido;

namespace Pedidos.Application.Interfaces.Pedidos;

/// <summary>
/// Consultas de solo lectura de pedidos. La define Application (lo que necesita) e Infrastructure la
/// implementa con proyecciones de EF Core al DTO, sin tracking.
/// </summary>
public interface IPedidosConsultas
{
    /// <summary>Un pedido con sus líneas y totales, o nulo si no existe. Proyección directa al DTO, sin tracking.</summary>
    Task<PedidoDto?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Listado paginado de resúmenes (sin tracking ni líneas), con filtro opcional por cliente.</summary>
    Task<PagedResult<PedidoResumenDto>> ListarAsync(
        int pagina, int tamanio, Guid? clienteId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pedidos creados entre dos fechas (ambas incluidas), con sus líneas y totales, del más nuevo al más viejo.
    /// Devuelve como máximo <paramref name="maximo"/> pedidos.
    /// </summary>
    Task<IReadOnlyList<PedidoDto>> ListarPorFechasAsync(
        DateOnly desde, DateOnly hasta, int maximo, CancellationToken cancellationToken = default);

    /// <summary>Pedidos con sus líneas en una sola consulta (evita el problema N+1).</summary>
    Task<IReadOnlyList<PedidoConLineasDto>> ListarConLineasAsync(
        int cantidad, CancellationToken cancellationToken = default);
}
