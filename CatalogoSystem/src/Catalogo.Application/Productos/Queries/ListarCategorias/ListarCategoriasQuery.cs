using Catalogo.Application.Interfaces;
using MediatR;

namespace Catalogo.Application.Productos.Queries.ListarCategorias;

public record ListarCategoriasQuery : IRequest<IReadOnlyList<string>>;

public sealed class ListarCategoriasQueryHandler(ICatalogoRepository catalogo)
    : IRequestHandler<ListarCategoriasQuery, IReadOnlyList<string>>
{
    public Task<IReadOnlyList<string>> Handle(ListarCategoriasQuery request, CancellationToken cancellationToken) =>
        catalogo.CategoriasAsync(cancellationToken);
}
