using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Pedidos.Application.Productos.Commands.ActualizarProducto;
using Pedidos.Application.Productos.Commands.CrearProducto;
using Pedidos.Application.Productos.Commands.EliminarProducto;
using Pedidos.Application.Productos.Queries.ListarProductos;
using Pedidos.Application.Productos.Queries.ObtenerProducto;

namespace Pedidos.Api.Controllers;

/// <summary>
/// Los productos no viven en Pedidos: este controlador llama al servicio Catálogo (CatalogoSystem) por REST.
/// No tiene lógica: envía cada request por MediatR (validación → handler) y devuelve la respuesta HTTP.
/// Un producto se identifica por categoría + id. Los errores los traduce <see cref="Infrastructure.GlobalExceptionHandler"/>.
/// </summary>
[ApiController]
[Route("api/productos")]
public sealed class ProductosController(ISender sender) : ControllerBase
{
    /// <summary>Crea un producto en el Catálogo.</summary>
    [HttpPost]
    [ProducesResponseType<ProductoDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Crear(CrearProductoCommand command, CancellationToken cancellationToken)
    {
        var producto = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(Obtener), new { categoria = producto.Categoria, id = producto.Id }, producto);
    }

    /// <summary>Lista los productos de una categoría (de a 20 por página).</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ProductoDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Listar(
        [FromQuery] string categoria, CancellationToken cancellationToken, [FromQuery] int pagina = 1) =>
        Ok(await sender.Send(new ListarProductosQuery(categoria, pagina), cancellationToken));

    /// <summary>Obtiene un producto.</summary>
    [HttpGet("{categoria}/{id}")]
    [ProducesResponseType<ProductoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Obtener(string categoria, string id, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new ObtenerProductoQuery(categoria, id), cancellationToken));

    /// <summary>Cambia nombre, precio y stock de un producto.</summary>
    [HttpPut("{categoria}/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Actualizar(
        string categoria, string id, ActualizarProductoRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(
            new ActualizarProductoCommand(categoria, id, request.Nombre, request.Precio, request.Stock), cancellationToken);
        return NoContent();
    }

    /// <summary>Elimina un producto.</summary>
    [HttpDelete("{categoria}/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Eliminar(string categoria, string id, CancellationToken cancellationToken)
    {
        await sender.Send(new EliminarProductoCommand(categoria, id), cancellationToken);
        return NoContent();
    }
}

/// <summary>Cuerpo de PUT /api/productos/{categoria}/{id}: la categoría y el id viajan en la ruta.</summary>
public sealed record ActualizarProductoRequest(
    string Nombre,
    [property: JsonRequired] decimal Precio,
    [property: JsonRequired] int Stock);
