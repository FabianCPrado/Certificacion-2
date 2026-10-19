using FluentValidation;

namespace Pedidos.Application.Productos.Commands.ActualizarProducto;

public sealed class ActualizarProductoCommandValidator : AbstractValidator<ActualizarProductoCommand>
{
    public ActualizarProductoCommandValidator()
    {
        RuleFor(c => c.ProductoId).NotEmpty();
        RuleFor(c => c.Nombre).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Categoria).NotEmpty().MaximumLength(50);
        RuleFor(c => c.Precio).GreaterThanOrEqualTo(0);
        RuleFor(c => c.Stock).GreaterThanOrEqualTo(0);
    }
}
