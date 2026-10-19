using Pedidos.Application.Interfaces.Demo;
using Pedidos.Application.Interfaces.Productos;
using Pedidos.Application.Interfaces.Storage;

namespace Pedidos.Api.Endpoints;

/// <summary>
/// Endpoints de demo de la Clase 5 (Redis y Blob Storage). Solo se mapean en Development.
/// Los endpoints solo conocen interfaces de Application; el código de Redis, Blob y Cosmos vive en Infrastructure.
/// Requieren los contenedores de docker-compose.yml (Redis, Azurite y Cosmos) y, para la caché, los datos de scripts/seed-clase4.sql.
/// </summary>
public static class DemoClase5Endpoints
{
    public static void MapDemoClase5(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/demo/clase5").WithTags("Demo Clase 5");

        MapRedis(grupo);
        MapBlob(grupo);
    }

    // 5.2 · Redis: cache-aside, lo malo contra lo bueno
    private static void MapRedis(RouteGroupBuilder g)
    {
        // MALO: sin expiración
        g.MapGet("/cache/malo/{categoria}/{id}", async (string categoria, string id, IDemoCache demo, CancellationToken ct) =>
            await demo.ObtenerMaloAsync(categoria, id, ct) is { } lectura ? Results.Ok(lectura) : Results.NotFound());

        // MALO: actualiza el precio y no invalida la caché
        g.MapPut("/cache/malo/{categoria}/{id}/precio/{precio:decimal}", async (string categoria, string id, decimal precio, IDemoCache demo, CancellationToken ct) =>
            await demo.ActualizarPrecioMaloAsync(categoria, id, precio, ct) ? Results.NoContent() : Results.NotFound());

        // BUENO: cache-aside con expiración
        g.MapGet("/cache/bueno/{categoria}/{id}", async (string categoria, string id, IDemoCache demo, CancellationToken ct) =>
            await demo.ObtenerBuenoAsync(categoria, id, ct) is { } lectura ? Results.Ok(lectura) : Results.NotFound());

        // BUENO: al actualizar, invalida la clave
        g.MapPut("/cache/bueno/{categoria}/{id}/precio/{precio:decimal}", async (string categoria, string id, decimal precio, IDemoCache demo, CancellationToken ct) =>
            await demo.ActualizarPrecioBuenoAsync(categoria, id, precio, ct) ? Results.NoContent() : Results.NotFound());
    }

    // 5.3 · Blob Storage: las imágenes de un producto. Un producto puede tener varias: cada una se guarda como
    // "{productoId}/{archivo}" y se buscan todas con el prefijo "{productoId}/". La base de datos no guarda las imágenes;
    // el producto es el del servicio Catálogo.
    private static void MapBlob(RouteGroupBuilder g)
    {
        // Agregar una imagen a un producto que existe
        g.MapPost("/imagenes/{categoria}/{productoId}", async (
            string categoria, string productoId, IFormFile archivo, ICatalogoProductos productos, IImagenesStorage storage, CancellationToken ct) =>
        {
            if (!archivo.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                return Results.Problem("El archivo debe ser una imagen.", statusCode: StatusCodes.Status400BadRequest);

            var producto = await productos.ObtenerAsync(categoria, productoId, ct);
            if (producto is null)
                return Results.Problem($"No existe el producto {productoId}.", statusCode: StatusCodes.Status404NotFound);

            // Se conserva el nombre del archivo ("bebidas-estante.jpg") para reconocerlo en el explorador de Blob.
            // Otro archivo con distinto nombre se agrega al producto; con el mismo nombre, reemplaza al anterior.
            var nombre = $"{productoId}/{Path.GetFileName(archivo.FileName)}";

            await using var contenido = archivo.OpenReadStream();
            var url = await storage.SubirAsync(nombre, contenido, archivo.ContentType, ct);
            return Results.Ok(new { productoId, nombreProducto = producto.Nombre, imagen = nombre, url });
        }).DisableAntiforgery();

        // Buscar todas las imágenes de un producto
        g.MapGet("/imagenes/{categoria}/{productoId}", async (
            string categoria, string productoId, ICatalogoProductos productos, IImagenesStorage storage, CancellationToken ct) =>
        {
            var producto = await productos.ObtenerAsync(categoria, productoId, ct);
            if (producto is null)
                return Results.Problem($"No existe el producto {productoId}.", statusCode: StatusCodes.Status404NotFound);

            var imagenes = await storage.ListarAsync($"{productoId}/", ct);
            return Results.Ok(new { productoId, nombreProducto = producto.Nombre, cantidad = imagenes.Count, imagenes });
        });
    }
}

