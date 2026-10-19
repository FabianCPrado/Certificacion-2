using Catalogo.Application.Interfaces;
using MediatR;

namespace Catalogo.Application.Productos.Queries.ObtenerProducto;

public record ObtenerProductoQuery(string Categoria, string Id) : IRequest<ProductoDto?>;

public sealed class ObtenerProductoQueryHandler(ICatalogoRepository catalogo) : IRequestHandler<ObtenerProductoQuery, ProductoDto?>
{
    public async Task<ProductoDto?> Handle(ObtenerProductoQuery request, CancellationToken cancellationToken)
    {
        var producto = await catalogo.ObtenerAsync(request.Id, request.Categoria, cancellationToken);
        return producto is null ? null : ProductoDto.Desde(producto);
    }
}
