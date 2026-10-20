using System.Diagnostics;
using Pedidos.Application.Interfaces.Catalogo;
using Pedidos.Application.Interfaces.Productos;

namespace Pedidos.Api.Endpoints;

/// <summary>Compara REST y gRPC para una lectura de precio y stock. Solo se expone en Development.</summary>
public static class DemoClase6Endpoints
{
    public static void MapDemoClase6(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/demo/clase6").WithTags("Demo Clase 6");

        group.MapGet("/catalogo-rest/{categoria}/{id}", ObtenerPorRestAsync)
            .WithName("DemoCatalogoRest")
            .WithSummary("Lee precio y stock de Catálogo por REST");

        group.MapGet("/catalogo-grpc/{categoria}/{id}", ObtenerPorGrpcAsync)
            .WithName("DemoCatalogoGrpc")
            .WithSummary("Lee precio y stock de Catálogo por gRPC");

        group.MapGet("/comparar/{categoria}/{id}", CompararAsync)
            .WithName("CompararCatalogoRestGrpc")
            .WithSummary("Compara una llamada REST y una gRPC a Catálogo");
    }

    private static async Task<IResult> ObtenerPorRestAsync(
        string categoria,
        string id,
        ICatalogoProductos productos,
        CancellationToken cancellationToken)
    {
        var timer = Stopwatch.StartNew();
        var producto = await productos.ObtenerAsync(categoria, id, cancellationToken);
        if (producto is null)
            return Results.NotFound();

        return Results.Ok(new
        {
            via = "REST",
            ms = timer.ElapsedMilliseconds,
            producto.Precio,
            producto.Stock
        });
    }

    private static async Task<IResult> ObtenerPorGrpcAsync(
        string categoria,
        string id,
        ICatalogoGrpcClient catalogo,
        CancellationToken cancellationToken)
    {
        var timer = Stopwatch.StartNew();
        var producto = await catalogo.ObtenerPrecioYStockAsync(categoria, id, cancellationToken);
        if (producto is null)
            return Results.NotFound();

        return Results.Ok(new
        {
            via = "gRPC",
            ms = timer.ElapsedMilliseconds,
            producto.Precio,
            producto.Stock
        });
    }

    private static async Task<IResult> CompararAsync(
        string categoria,
        string id,
        ICatalogoProductos rest,
        ICatalogoGrpcClient grpc,
        CancellationToken cancellationToken)
    {
        var restTimer = Stopwatch.StartNew();
        var restProduct = await rest.ObtenerAsync(categoria, id, cancellationToken);
        var restMs = restTimer.ElapsedMilliseconds;
        if (restProduct is null)
            return Results.NotFound(new { via = "REST", mensaje = "Producto no encontrado" });

        var grpcTimer = Stopwatch.StartNew();
        var grpcProduct = await grpc.ObtenerPrecioYStockAsync(categoria, id, cancellationToken);
        var grpcMs = grpcTimer.ElapsedMilliseconds;
        if (grpcProduct is null)
            return Results.NotFound(new { via = "gRPC", mensaje = "Producto no encontrado" });

        return Results.Ok(new
        {
            productoId = id,
            rest = new { ms = restMs, restProduct.Precio, restProduct.Stock },
            grpc = new { ms = grpcMs, grpcProduct.Precio, grpcProduct.Stock }
        });
    }
}