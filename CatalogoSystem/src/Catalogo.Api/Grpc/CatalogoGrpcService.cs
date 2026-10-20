using Catalogo.Application.Productos.Queries.ObtenerProducto;
using Catalogo.Grpc;
using Grpc.Core;
using MediatR;
using Microsoft.AspNetCore.Authorization;

namespace Catalogo.Api.Grpc;

/// <summary>Segunda puerta al catálogo: usa la misma query que la Minimal API y no tiene lógica propia.</summary>
[Authorize(Policy = "PedidosService")]
public class CatalogoGrpcService(ISender sender) : CatalogoService.CatalogoServiceBase
{
    public override async Task<PrecioYStockReply> ObtenerPrecioYStock(ProductoRequest request, ServerCallContext context)
    {
        var p = await sender.Send(new ObtenerProductoQuery(request.Categoria, request.ProductoId), context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Producto {request.ProductoId} inexistente"));

        return new PrecioYStockReply { ProductoId = p.Id, Precio = (double)p.Precio, Stock = p.Stock };
    }

    // Streaming del servidor
    public override async Task SeguirStock(ProductoRequest request, IServerStreamWriter<StockReply> responseStream, ServerCallContext context)
    {
        for (var i = 0; i < 5 && !context.CancellationToken.IsCancellationRequested; i++)
        {
            var p = await sender.Send(new ObtenerProductoQuery(request.Categoria, request.ProductoId), context.CancellationToken);
            await responseStream.WriteAsync(new StockReply { Stock = p?.Stock ?? 0 }, context.CancellationToken);
            await Task.Delay(2000, context.CancellationToken);
        }
    }
}
