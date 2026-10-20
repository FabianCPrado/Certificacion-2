using Catalogo.Application.Productos.Queries.ObtenerProducto;
using MediatR;

namespace Catalogo.Api.Endpoints;

/// <summary>Solo para la clase (6.2): se mapea únicamente en Development.</summary>
public static class DemoClase6Endpoints
{
    public static void MapDemoClase6(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/demo/clase6")
            .WithTags("Demo Clase 6")
            .RequireAuthorization("PedidosService");

        // LO MALO: IResult sin tipo; Swagger no sabe qué devuelve
        g.MapGet("/producto-malo/{categoria}/{id}", async (string categoria, string id, ISender sender, CancellationToken ct) =>
        {
            var p = await sender.Send(new ObtenerProductoQuery(categoria, id), ct);
            return p is null ? Results.NotFound() : Results.Ok(p);
        });
    }
}
