using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Pedidos.Application.Common;
using Pedidos.Application.Productos.Commands.ActualizarProducto;
using Pedidos.Application.Productos.Commands.CrearProducto;
using Pedidos.Application.Productos.Commands.EliminarProducto;
using Pedidos.Application.Productos.Queries.ListarProductos;
using Pedidos.Application.Productos.Queries.ObtenerProducto;

namespace Pedidos.Api.Controllers;

/// <summary>
/// El controlador no tiene lógica: envía cada request por MediatR (validación → handler)
/// y devuelve la respuesta HTTP. Los errores los traduce <see cref="Infrastructure.GlobalExceptionHandler"/>.
/// </summary>
[ApiController]
[Route("api/productos")]
public sealed class ProductosController(ISender sender) : ControllerBase
{
    /// <summary>Crea un producto.</summary>
    [HttpPost]
    [ProducesResponseType<Guid>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Crear(CrearProductoCommand command, CancellationToken cancellationToken)
    {
        var id = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(Obtener), new { id }, id);
    }

    /// <summary>Lista los productos (paginado), opcionalmente por categoría y precio mínimo.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<ProductoDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Listar(
        [FromQuery] string? categoria, [FromQuery] decimal? precioMinimo, CancellationToken cancellationToken,
        [FromQuery] int pagina = 1, [FromQuery] int tamanio = 20)
    {
        var productos = await sender.Send(
            new ListarProductosQuery(pagina, tamanio, categoria, precioMinimo), cancellationToken);
        return Ok(productos);
    }

    /// <summary>Obtiene un producto.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<ProductoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(Guid id, CancellationToken cancellationToken)
    {
        var producto = await sender.Send(new ObtenerProductoQuery(id), cancellationToken);
        return Ok(producto);
    }

    /// <summary>Cambia los datos de un producto.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Actualizar(
        Guid id, ActualizarProductoRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(
            new ActualizarProductoCommand(id, request.Nombre, request.Categoria, request.Precio, request.Stock),
            cancellationToken);
        return NoContent();
    }

    /// <summary>Elimina un producto.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new EliminarProductoCommand(id), cancellationToken);
        return NoContent();
    }
}

/// <summary>Cuerpo de PUT /api/productos/{id}: el Id viaja en la ruta.</summary>
public sealed record ActualizarProductoRequest(
    string Nombre,
    string Categoria,
    [property: JsonRequired] decimal Precio,
    [property: JsonRequired] int Stock);
