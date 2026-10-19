using FluentValidation;
using MediatR;
using Pedidos.Application.Interfaces.Productos;
using Pedidos.Application.Common;

namespace Pedidos.Application.Productos.Commands.AumentarPrecioCategoria;

/// <summary>Sube el precio de toda una categoría en un porcentaje. Devuelve las filas afectadas.</summary>
public sealed record AumentarPrecioCategoriaCommand(string Categoria, decimal Porcentaje) : ICommand<int>;

public sealed class AumentarPrecioCategoriaCommandValidator : AbstractValidator<AumentarPrecioCategoriaCommand>
{
    public AumentarPrecioCategoriaCommandValidator()
    {
        RuleFor(c => c.Categoria).NotEmpty().MaximumLength(50);
        RuleFor(c => c.Porcentaje).GreaterThan(0).LessThanOrEqualTo(100);
    }
}

public sealed class AumentarPrecioCategoriaCommandHandler(IProductoRepository productos)
    : IRequestHandler<AumentarPrecioCategoriaCommand, int>
{
    public Task<int> Handle(AumentarPrecioCategoriaCommand request, CancellationToken cancellationToken) =>
        productos.AumentarPrecioPorCategoriaAsync(request.Categoria, request.Porcentaje, cancellationToken);
}
