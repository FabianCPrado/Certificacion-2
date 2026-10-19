using Catalogo.Application.Interfaces;
using MediatR;

namespace Catalogo.Application.Productos.Queries.ListarProductos;

public record ListarProductosQuery(string Categoria, int Pagina) : IRequest<IReadOnlyList<ProductoDto>>;

public sealed class ListarProductosQueryHandler(ICatalogoRepository catalogo)
    : IRequestHandler<ListarProductosQuery, IReadOnlyList<ProductoDto>>
{
    public const int TamanoPagina = 20;

    public async Task<IReadOnlyList<ProductoDto>> Handle(ListarProductosQuery request, CancellationToken cancellationToken)
    {
        var saltar = (Math.Max(request.Pagina, 1) - 1) * TamanoPagina;
        var productos = await catalogo.PorCategoriaAsync(request.Categoria, saltar, TamanoPagina, cancellationToken);
        return productos.Select(ProductoDto.Desde).ToList();
    }
}
