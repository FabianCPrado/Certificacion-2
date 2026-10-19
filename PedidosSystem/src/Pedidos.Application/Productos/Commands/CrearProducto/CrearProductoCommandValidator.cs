using FluentValidation;

namespace Pedidos.Application.Productos.Commands.CrearProducto;

/// <summary>Validación de forma (datos de entrada). Las reglas de negocio viven en el repositorio.</summary>
public sealed class CrearProductoCommandValidator : AbstractValidator<CrearProductoCommand>
{
    public CrearProductoCommandValidator()
    {
        RuleFor(c => c.Nombre).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Categoria).NotEmpty().MaximumLength(50);
        RuleFor(c => c.Precio).GreaterThanOrEqualTo(0);
        RuleFor(c => c.Stock).GreaterThanOrEqualTo(0);
    }
}
