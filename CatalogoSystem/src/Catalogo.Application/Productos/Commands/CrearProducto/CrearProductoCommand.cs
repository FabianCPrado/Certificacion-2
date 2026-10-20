using Catalogo.Application.Interfaces;
using Catalogo.Domain.Productos;
using FluentValidation;
using MediatR;

namespace Catalogo.Application.Productos.Commands.CrearProducto;

public record CrearProductoCommand(string Categoria, string Nombre, decimal Precio, int Stock, IReadOnlyList<AtributoDto>? Atributos = null)
    : IRequest<ProductoDto>;

public sealed class CrearProductoValidator : AbstractValidator<CrearProductoCommand>
{
    public CrearProductoValidator()
    {
        RuleFor(c => c.Categoria).NotEmpty().MaximumLength(100);
        RuleFor(c => c.Nombre).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Precio).GreaterThan(0);
        RuleFor(c => c.Stock).GreaterThanOrEqualTo(0);
        RuleForEach(c => c.Atributos).ChildRules(atributo =>
        {
            atributo.RuleFor(a => a.Parametro).NotEmpty().MaximumLength(200);
            atributo.RuleFor(a => a.Valor).NotEmpty();
        });
        RuleFor(c => c.Atributos)
            .Must(atributos => atributos is null || atributos.Select(a => a.Parametro).Distinct(StringComparer.OrdinalIgnoreCase).Count() == atributos.Count)
            .WithMessage("Los parámetros de atributos no se pueden repetir.");
    }
}

public sealed class CrearProductoCommandHandler(ICatalogoRepository catalogo) : IRequestHandler<CrearProductoCommand, ProductoDto>
{
    public async Task<ProductoDto> Handle(CrearProductoCommand request, CancellationToken cancellationToken)
    {
        var producto = new ProductoCatalogo
        {
            Categoria = request.Categoria,
            Nombre = request.Nombre,
            Precio = request.Precio,
            Stock = request.Stock,
            Atributos = request.Atributos?.Select(a => new ProductoAtributo
            {
                Parametro = a.Parametro,
                Valor = a.Valor
            }).ToList() ?? []
        };

        await catalogo.GuardarAsync(producto, cancellationToken);
        return ProductoDto.Desde(producto);
    }
}
