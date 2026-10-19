using FluentValidation;

namespace Pedidos.Application.Pedidos.Commands.CrearPedido;

/// <summary>Validación de forma (datos de entrada). Las reglas de negocio viven en el agregado.</summary>
public sealed class CrearPedidoCommandValidator : AbstractValidator<CrearPedidoCommand>
{
    public CrearPedidoCommandValidator()
    {
        RuleFor(c => c.ClienteId).NotEmpty();
        RuleFor(c => c.EmailCliente).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(c => c.Moneda).NotEmpty().Length(3);

        RuleFor(c => c.Direccion).NotNull().ChildRules(direccion =>
        {
            direccion.RuleFor(d => d.Calle).NotEmpty().MaximumLength(200);
            direccion.RuleFor(d => d.Ciudad).NotEmpty().MaximumLength(100);
            direccion.RuleFor(d => d.CodigoPostal).MaximumLength(20);
            direccion.RuleFor(d => d.Pais).NotEmpty().MaximumLength(100);
        });

        RuleFor(c => c.Lineas).NotEmpty().WithMessage("El pedido debe tener al menos una línea.");

        RuleForEach(c => c.Lineas).ChildRules(linea =>
        {
            linea.RuleFor(l => l.ProductoId).NotEmpty();
            linea.RuleFor(l => l.Categoria).NotEmpty().MaximumLength(100);
            linea.RuleFor(l => l.Cantidad).GreaterThan(0);
        });
    }
}
