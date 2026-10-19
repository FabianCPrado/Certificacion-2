using Microsoft.AspNetCore.Mvc;
using Pedidos.Application.Interfaces.Productos;
using Pedidos.Application.Interfaces.Storage;

namespace Pedidos.Api.Controllers;

/// <summary>Blob Storage: las imágenes de un producto. Se guardan como "{productoId}/{archivo}"; la base de datos no las guarda.</summary>
[ApiController]
[Route("api/blob")]
[Tags("Blob Storage")]
public sealed class BlobController(IProductoConsultas productos, IImagenesStorage storage) : ControllerBase
{
    /// <summary>Agrega una imagen a un producto. Con el mismo nombre de archivo reemplaza a la anterior.</summary>
    [HttpPost("imagenes/{productoId:guid}")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Subir(Guid productoId, IFormFile archivo, CancellationToken cancellationToken)
    {
        if (!archivo.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return Problem("El archivo debe ser una imagen.", statusCode: StatusCodes.Status400BadRequest);

        var producto = await productos.ObtenerPorIdAsync(productoId, cancellationToken);
        if (producto is null)
            return Problem($"No existe el producto {productoId}.", statusCode: StatusCodes.Status404NotFound);

        var nombre = $"{productoId}/{Path.GetFileName(archivo.FileName)}";

        await using var contenido = archivo.OpenReadStream();
        var url = await storage.SubirAsync(nombre, contenido, archivo.ContentType, cancellationToken);
        return Ok(new { productoId, nombreProducto = producto.Nombre, imagen = nombre, url });
    }

    /// <summary>Lista todas las imágenes de un producto.</summary>
    [HttpGet("imagenes/{productoId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Listar(Guid productoId, CancellationToken cancellationToken)
    {
        var producto = await productos.ObtenerPorIdAsync(productoId, cancellationToken);
        if (producto is null)
            return Problem($"No existe el producto {productoId}.", statusCode: StatusCodes.Status404NotFound);

        var imagenes = await storage.ListarAsync($"{productoId}/", cancellationToken);
        return Ok(new { productoId, nombreProducto = producto.Nombre, cantidad = imagenes.Count, imagenes });
    }
}
