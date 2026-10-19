using Catalogo.Application.Interfaces;
using FluentValidation;
using MediatR;

namespace Catalogo.Application.Productos.Queries.BuscarProductos;

public record BuscarProductosQuery(string Categoria, string Nombre) : IRequest<IReadOnlyList<ProductoDto>>;

public sealed class BuscarProductosQueryValidator : AbstractValidator<BuscarProductosQuery>
{
    public BuscarProductosQueryValidator()
    {
        RuleFor(q => q.Categoria).NotEmpty();
        RuleFor(q => q.Nombre).NotEmpty().MaximumLength(100);
    }
}

public sealed class BuscarProductosQueryHandler(ICatalogoRepository catalogo)
    : IRequestHandler<BuscarProductosQuery, IReadOnlyList<ProductoDto>>
{
    public async Task<IReadOnlyList<ProductoDto>> Handle(BuscarProductosQuery request, CancellationToken cancellationToken)
    {
        var productos = await catalogo.BuscarPorNombreAsync(request.Categoria, request.Nombre, cancellationToken);
        return productos.Select(ProductoDto.Desde).ToList();
    }
}
