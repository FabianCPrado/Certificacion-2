using FluentValidation;
using MediatR;
using Pedidos.Application.Common;
using Pedidos.Application.Interfaces.Productos;
using Pedidos.Application.Productos.Queries.ObtenerProducto;

namespace Pedidos.Application.Productos.Queries.ListarProductos;

public sealed record ListarProductosQuery(string Categoria, int Pagina = 1) : IQuery<IReadOnlyList<ProductoDto>>;

public sealed class ListarProductosQueryValidator : AbstractValidator<ListarProductosQuery>
{
    public ListarProductosQueryValidator()
    {
        RuleFor(q => q.Categoria).NotEmpty();
        RuleFor(q => q.Pagina).GreaterThanOrEqualTo(1);
    }
}

public sealed class ListarProductosQueryHandler(ICatalogoProductos catalogo)
    : IRequestHandler<ListarProductosQuery, IReadOnlyList<ProductoDto>>
{
    public Task<IReadOnlyList<ProductoDto>> Handle(ListarProductosQuery request, CancellationToken cancellationToken) =>
        catalogo.ListarAsync(request.Categoria, request.Pagina, cancellationToken);
}
