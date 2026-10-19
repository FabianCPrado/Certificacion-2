using FluentValidation;
using MediatR;
using Pedidos.Application.Interfaces.Pedidos;
using Pedidos.Application.Pedidos.Commands.CrearPedido;
using Pedidos.Application.Common;

namespace Pedidos.Application.Pedidos.Commands.CrearPedidoConStock;

/// <summary>
/// Crea el pedido con sus líneas y descuenta el stock de cada producto en una sola transacción:
/// si falta stock de cualquier línea, no queda nada guardado. Devuelve el Id del pedido.
/// </summary>
public sealed record CrearPedidoConStockCommand(CrearPedidoCommand Pedido) : ICommand<Guid>;

public sealed class CrearPedidoConStockCommandValidator : AbstractValidator<CrearPedidoConStockCommand>
{
    public CrearPedidoConStockCommandValidator() =>
        RuleFor(c => c.Pedido).NotNull().SetValidator(new CrearPedidoCommandValidator());
}

public sealed class CrearPedidoConStockCommandHandler(IPedidoRepository pedidos, TimeProvider tiempo)
    : IRequestHandler<CrearPedidoConStockCommand, Guid>
{
    public async Task<Guid> Handle(CrearPedidoConStockCommand request, CancellationToken cancellationToken)
    {
        var comando = request.Pedido;

        // La transacción vive en el repositorio (Infrastructure): aquí solo se pide el caso de uso.
        var pedido = await pedidos.CrearYDescontarStockAsync(
            comando.ClienteId, comando.ToEmail(), comando.ToDireccion(), comando.Moneda,
            tiempo.GetUtcNow().UtcDateTime, comando.ToLineasNuevas(), cancellationToken);

        return pedido.Id;
    }
}
