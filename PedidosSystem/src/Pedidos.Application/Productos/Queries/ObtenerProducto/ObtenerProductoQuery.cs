using MediatR;
using Pedidos.Application.Common;
using Pedidos.Application.Exceptions;
using Pedidos.Application.Interfaces.Productos;

namespace Pedidos.Application.Productos.Queries.ObtenerProducto;

public sealed record ObtenerProductoQuery(string Categoria, string ProductoId) : IQuery<ProductoDto>;

/// <summary>Consulta: lee el producto del servicio Catálogo.</summary>
public sealed class ObtenerProductoQueryHandler(ICatalogoProductos catalogo)
    : IRequestHandler<ObtenerProductoQuery, ProductoDto>
{
    public async Task<ProductoDto> Handle(ObtenerProductoQuery request, CancellationToken cancellationToken) =>
        await catalogo.ObtenerAsync(request.Categoria, request.ProductoId, cancellationToken)
            ?? throw new NotFoundException($"No existe el producto {request.ProductoId} en la categoría {request.Categoria}.");
}
