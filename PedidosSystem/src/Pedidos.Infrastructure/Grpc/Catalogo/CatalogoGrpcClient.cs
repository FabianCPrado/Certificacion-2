using Catalogo.Grpc;
using Grpc.Core;
using Pedidos.Application.Interfaces.Catalogo;

namespace Pedidos.Infrastructure.Grpc.Catalogo;

public sealed class CatalogoGrpcClient(CatalogoService.CatalogoServiceClient client) : ICatalogoGrpcClient
{
    public async Task<PrecioYStockCatalogo?> ObtenerPrecioYStockAsync(
        string categoria,
        string productoId,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await client.ObtenerPrecioYStockAsync(
                new ProductoRequest { Categoria = categoria, ProductoId = productoId },
                cancellationToken: cancellationToken);
            return new PrecioYStockCatalogo((decimal)response.Precio, response.Stock);
        }
        catch (RpcException exception) when (exception.StatusCode == StatusCode.NotFound)
        {
            return null;
        }
    }
}