using FluentValidation;

namespace Pedidos.Application.Productos.Commands.ActualizarProducto;

public sealed class ActualizarProductoCommandValidator : AbstractValidator<ActualizarProductoCommand>
{
    public ActualizarProductoCommandValidator()
    {
        RuleFor(c => c.Categoria).NotEmpty();
        RuleFor(c => c.ProductoId).NotEmpty();
        RuleFor(c => c.Nombre).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Precio).GreaterThan(0);
        RuleFor(c => c.Stock).GreaterThanOrEqualTo(0);
    }
}
