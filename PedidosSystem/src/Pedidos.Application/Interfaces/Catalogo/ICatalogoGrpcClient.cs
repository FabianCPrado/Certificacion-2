namespace Pedidos.Application.Interfaces.Catalogo;

public sealed record PrecioYStockCatalogo(decimal Precio, int Stock);

public interface ICatalogoGrpcClient
{
    Task<PrecioYStockCatalogo?> ObtenerPrecioYStockAsync(
        string categoria,
        string productoId,
        CancellationToken cancellationToken);
}