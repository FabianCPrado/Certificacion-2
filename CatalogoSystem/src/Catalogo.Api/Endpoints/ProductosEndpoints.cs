using Catalogo.Api.Filtros;
using Catalogo.Application.Productos;
using Catalogo.Application.Productos.Commands.ActualizarProducto;
using Catalogo.Application.Productos.Commands.AjustarStock;
using Catalogo.Application.Productos.Commands.AumentarPrecioCategoria;
using Catalogo.Application.Productos.Commands.CambiarPrecio;
using Catalogo.Application.Productos.Commands.CrearProducto;
using Catalogo.Application.Productos.Commands.EliminarProducto;
using Catalogo.Application.Productos.Queries.BuscarProductos;
using Catalogo.Application.Productos.Queries.ListarCategorias;
using Catalogo.Application.Productos.Queries.ListarProductos;
using Catalogo.Application.Productos.Queries.ListarTodosProductos;
using Catalogo.Application.Productos.Queries.ObtenerProducto;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Catalogo.Api.Endpoints;

public static class ProductosEndpoints
{
    public record CambiarPrecioRequest(decimal Precio);
    public record CrearProductoRequest(string Categoria, string Nombre, decimal Precio, int Stock, IReadOnlyList<AtributoDto>? Atributos);
    public record ActualizarProductoRequest(string Nombre, decimal Precio, int Stock);
    public record StockRequest(int Cantidad);
    public record AumentarPrecioRequest(decimal Porcentaje);

    public static IEndpointRouteBuilder MapProductos(this IEndpointRouteBuilder app)
    {
        var raiz = app.MapGroup("/api/productos")
                      .WithTags("Productos")
                      .AddEndpointFilter<TiempoFilter>();

        // Lectura: la pueden pedir pedidos-service y asistente-service. Escritura: solo pedidos-service.
        var g = raiz.MapGroup("").RequireAuthorization("LecturaCatalogo");
        var escritura = raiz.MapGroup("").RequireAuthorization("PedidosService");

        g.MapGet("/", ListarAsync)
            .WithName("ListarProductos")
            .WithSummary("Lista los productos de una categoría, de a 20 por página");

        g.MapGet("/categorias", ListarCategoriasAsync)
            .WithName("ListarCategorias")
            .WithSummary("Lista las categorías que tienen productos");

        g.MapGet("/todos", ListarTodosAsync)
            .WithName("ListarTodosLosProductos")
            .WithSummary("Lista los productos de todas las categorías, paginado (hasta 100 por página)");

        g.MapGet("/buscar", BuscarAsync)
            .WithName("BuscarProductos")
            .WithSummary("Busca productos por nombre dentro de una categoría");

        g.MapGet("/{categoria}/{id}", ObtenerAsync)
            .WithName("ObtenerProducto")
            .WithSummary("Obtiene un producto por categoría e id")
            .WithDescription("La categoría y el id identifican el producto.");

        escritura.MapPut("/{categoria}/{id}/precio", CambiarPrecioAsync)
            .WithName("CambiarPrecio")
            .WithSummary("Cambia el precio de un producto")
            .WithDescription("El precio debe ser mayor que 0; si no, responde 400.");

        escritura.MapPost("/", CrearAsync)
            .WithName("CrearProducto")
            .WithSummary("Crea un producto");

        escritura.MapPut("/{categoria}/{id}", ActualizarAsync)
            .WithName("ActualizarProducto")
            .WithSummary("Cambia nombre, precio y stock de un producto");

        escritura.MapDelete("/{categoria}/{id}", EliminarAsync)
            .WithName("EliminarProducto")
            .WithSummary("Elimina un producto");

        escritura.MapPost("/{categoria}/{id}/stock/descontar", DescontarStockAsync)
            .WithName("DescontarStock")
            .WithSummary("Descuenta stock de forma atómica")
            .WithDescription("404 si el producto no existe; 409 si no alcanza el stock.");

        escritura.MapPost("/{categoria}/{id}/stock/reponer", ReponerStockAsync)
            .WithName("ReponerStock")
            .WithSummary("Repone stock (compensa un descuento)");

        escritura.MapPost("/{categoria}/aumentar-precio", AumentarPrecioAsync)
            .WithName("AumentarPrecioCategoria")
            .WithSummary("Sube el precio de toda una categoría un porcentaje");

        return app;
    }

    private static async Task<Created<ProductoDto>> CrearAsync(CrearProductoRequest body, ISender sender, CancellationToken ct)
    {
        var p = await sender.Send(new CrearProductoCommand(body.Categoria, body.Nombre, body.Precio, body.Stock, body.Atributos), ct);
        return TypedResults.Created($"/api/productos/{p.Categoria}/{p.Id}", p);
    }

    private static async Task<Results<NoContent, NotFound>> ActualizarAsync(
        string categoria, string id, ActualizarProductoRequest body, ISender sender, CancellationToken ct)
        => await sender.Send(new ActualizarProductoCommand(categoria, id, body.Nombre, body.Precio, body.Stock), ct)
            ? TypedResults.NoContent()
            : TypedResults.NotFound();

    private static async Task<Results<NoContent, NotFound>> EliminarAsync(
        string categoria, string id, ISender sender, CancellationToken ct)
        => await sender.Send(new EliminarProductoCommand(categoria, id), ct)
            ? TypedResults.NoContent()
            : TypedResults.NotFound();

    private static async Task<NoContent> DescontarStockAsync(
        string categoria, string id, StockRequest body, ISender sender, CancellationToken ct)
    {
        await sender.Send(new AjustarStockCommand(categoria, id, body.Cantidad, Descontar: true), ct);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> ReponerStockAsync(
        string categoria, string id, StockRequest body, ISender sender, CancellationToken ct)
    {
        await sender.Send(new AjustarStockCommand(categoria, id, body.Cantidad, Descontar: false), ct);
        return TypedResults.NoContent();
    }

    private static async Task<Ok<int>> AumentarPrecioAsync(
        string categoria, AumentarPrecioRequest body, ISender sender, CancellationToken ct)
        => TypedResults.Ok(await sender.Send(new AumentarPrecioCategoriaCommand(categoria, body.Porcentaje), ct));

    // binding: categoria y pagina de la query, sender de DI, ct del framework
    private static async Task<Ok<IReadOnlyList<ProductoDto>>> ListarAsync(
        string categoria, ISender sender, CancellationToken ct, int pagina = 1)
        => TypedResults.Ok(await sender.Send(new ListarProductosQuery(categoria, pagina), ct));

    private static async Task<Ok<IReadOnlyList<string>>> ListarCategoriasAsync(ISender sender, CancellationToken ct)
        => TypedResults.Ok(await sender.Send(new ListarCategoriasQuery(), ct));

    // binding: pagina y tamanio de la query, con valores por defecto
    private static async Task<Ok<IReadOnlyList<ProductoDto>>> ListarTodosAsync(
        ISender sender, CancellationToken ct, int pagina = 1, int tamanio = 100)
        => TypedResults.Ok(await sender.Send(new ListarTodosProductosQuery(pagina, tamanio), ct));

    // binding: categoria y nombre de la query
    private static async Task<Ok<IReadOnlyList<ProductoDto>>> BuscarAsync(
        string categoria, string nombre, ISender sender, CancellationToken ct)
        => TypedResults.Ok(await sender.Send(new BuscarProductosQuery(categoria, nombre), ct));

    // binding: categoria e id de la ruta
    private static async Task<Results<Ok<ProductoDto>, NotFound>> ObtenerAsync(
        string categoria, string id, ISender sender, CancellationToken ct)
        => await sender.Send(new ObtenerProductoQuery(categoria, id), ct) is { } p
            ? TypedResults.Ok(p)
            : TypedResults.NotFound();

    // binding: el cuerpo en JSON
    private static async Task<Results<NoContent, NotFound>> CambiarPrecioAsync(
        string categoria, string id, CambiarPrecioRequest body, ISender sender, CancellationToken ct)
        => await sender.Send(new CambiarPrecioCommand(categoria, id, body.Precio), ct)
            ? TypedResults.NoContent()
            : TypedResults.NotFound();
}

