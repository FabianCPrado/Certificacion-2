using FluentValidation;
using MediatR;
using Pedidos.Application.Common;
using Pedidos.Application.Interfaces.Productos;

namespace Pedidos.Application.Productos.Commands.DescontarStock;

public sealed record DescontarStockCommand(string Categoria, string ProductoId, int Cantidad) : ICommand;

public sealed class DescontarStockCommandValidator : AbstractValidator<DescontarStockCommand>
{
    public DescontarStockCommandValidator()
    {
        RuleFor(c => c.Categoria).NotEmpty();
        RuleFor(c => c.ProductoId).NotEmpty();
        RuleFor(c => c.Cantidad).GreaterThan(0);
    }
}

public sealed class DescontarStockCommandHandler(ICatalogoProductos catalogo)
    : IRequestHandler<DescontarStockCommand>
{
    public Task Handle(DescontarStockCommand request, CancellationToken cancellationToken) =>
        catalogo.DescontarStockAsync(request.Categoria, request.ProductoId, request.Cantidad, cancellationToken);
}
