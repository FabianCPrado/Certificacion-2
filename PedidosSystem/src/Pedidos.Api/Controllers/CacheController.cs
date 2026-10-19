using MediatR;
using Microsoft.AspNetCore.Mvc;
using Pedidos.Application.Pedidos.Commands.InvalidarCachePedidos;
using Pedidos.Application.Pedidos.Queries.ObtenerPedidosPorFechasCacheado;
using Pedidos.Application.Productos.Commands.InvalidarCacheProducto;
using Pedidos.Application.Productos.Queries.ObtenerProductoCacheado;

namespace Pedidos.Api.Controllers;

/// <summary>Demo de Redis: las mismas consultas de productos y pedidos, pero pasando primero por el caché.</summary>
[ApiController]
[Route("api/cache")]
public sealed class CacheController(ISender sender) : ControllerBase
{
    /// <summary>Pedidos (con sus productos) creados entre dos fechas. La primera vez viene de SQL; las siguientes, de Redis.</summary>
    [HttpGet("pedidos")]
    [ProducesResponseType<PedidosPorFechasCacheadosDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ListarPedidosPorFechas(
        [FromQuery] DateOnly desde, [FromQuery] DateOnly hasta, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new ObtenerPedidosPorFechasCacheadoQuery(desde, hasta), cancellationToken));

    /// <summary>Saca del caché el resultado de ese rango de fechas.</summary>
    [HttpDelete("pedidos")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> InvalidarPedidosPorFechas(
        [FromQuery] DateOnly desde, [FromQuery] DateOnly hasta, CancellationToken cancellationToken)
    {
        await sender.Send(new InvalidarCachePedidosCommand(desde, hasta), cancellationToken);
        return NoContent();
    }

    /// <summary>Obtiene un producto. La primera vez viene de SQL ("sql"); las siguientes, de Redis ("redis").</summary>
    [HttpGet("productos/{id:guid}")]
    [ProducesResponseType<ProductoCacheadoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(Guid id, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new ObtenerProductoCacheadoQuery(id), cancellationToken));

    /// <summary>Saca el producto del caché: la siguiente lectura vuelve a SQL.</summary>
    [HttpDelete("productos/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Invalidar(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new InvalidarCacheProductoCommand(id), cancellationToken);
        return NoContent();
    }
}
