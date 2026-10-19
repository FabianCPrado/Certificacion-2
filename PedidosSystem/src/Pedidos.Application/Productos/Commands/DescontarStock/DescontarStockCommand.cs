using FluentValidation;
using MediatR;
using Pedidos.Application.Exceptions;
using Pedidos.Application.Interfaces.Productos;
using Pedidos.Application.Common;

namespace Pedidos.Application.Productos.Commands.DescontarStock;

public sealed record DescontarStockCommand(Guid ProductoId, int Cantidad) : ICommand;

public sealed class DescontarStockCommandValidator : AbstractValidator<DescontarStockCommand>
{
    public DescontarStockCommandValidator()
    {
        RuleFor(c => c.ProductoId).NotEmpty();
        RuleFor(c => c.Cantidad).GreaterThan(0);
    }
}

public sealed class DescontarStockCommandHandler(IProductoRepository productos)
    : IRequestHandler<DescontarStockCommand>
{
    public async Task Handle(DescontarStockCommand request, CancellationToken cancellationToken)
    {
        var producto = await productos.ObtenerPorIdAsync(request.ProductoId, cancellationToken)
            ?? throw new NotFoundException($"No existe el producto {request.ProductoId}.");

        await productos.DescontarStockAsync(producto, request.Cantidad, cancellationToken);
    }
}
