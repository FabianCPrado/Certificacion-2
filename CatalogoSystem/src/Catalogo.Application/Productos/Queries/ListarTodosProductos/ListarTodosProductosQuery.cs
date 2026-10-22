using Catalogo.Application.Interfaces;
using FluentValidation;
using MediatR;

namespace Catalogo.Application.Productos.Queries.ListarTodosProductos;

public record ListarTodosProductosQuery(int Pagina = 1, int Tamanio = 100) : IRequest<IReadOnlyList<ProductoDto>>;

public sealed class ListarTodosProductosQueryValidator : AbstractValidator<ListarTodosProductosQuery>
{
    public ListarTodosProductosQueryValidator()
    {
        RuleFor(q => q.Pagina).GreaterThanOrEqualTo(1);
        RuleFor(q => q.Tamanio).InclusiveBetween(1, 100);
    }
}

public sealed class ListarTodosProductosQueryHandler(ICatalogoRepository catalogo)
    : IRequestHandler<ListarTodosProductosQuery, IReadOnlyList<ProductoDto>>
{
    public async Task<IReadOnlyList<ProductoDto>> Handle(ListarTodosProductosQuery request, CancellationToken cancellationToken)
    {
        var saltar = (request.Pagina - 1) * request.Tamanio;
        var productos = await catalogo.TodosAsync(saltar, request.Tamanio, cancellationToken);
        return productos.Select(ProductoDto.Desde).ToList();
    }
}
