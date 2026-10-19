using FluentValidation;
using MediatR;
using Pedidos.Application.Common;
using Pedidos.Application.Interfaces.Productos;
using Pedidos.Application.Productos.Queries.ObtenerProducto;

namespace Pedidos.Application.Productos.Queries.ListarProductos;

public sealed record ListarProductosQuery(
    int Pagina = 1, int Tamanio = 20, string? Categoria = null, decimal? PrecioMinimo = null)
    : IQuery<PagedResult<ProductoDto>>;

public sealed class ListarProductosQueryValidator : AbstractValidator<ListarProductosQuery>
{
    public ListarProductosQueryValidator()
    {
        RuleFor(q => q.Pagina).GreaterThanOrEqualTo(1);
        RuleFor(q => q.Tamanio).InclusiveBetween(1, 100);
        RuleFor(q => q.PrecioMinimo).GreaterThanOrEqualTo(0);
    }
}

public sealed class ListarProductosQueryHandler(IProductoConsultas consultas)
    : IRequestHandler<ListarProductosQuery, PagedResult<ProductoDto>>
{
    public Task<PagedResult<ProductoDto>> Handle(ListarProductosQuery request, CancellationToken cancellationToken) =>
        consultas.ListarAsync(request.Pagina, request.Tamanio, request.Categoria, request.PrecioMinimo, cancellationToken);
}
