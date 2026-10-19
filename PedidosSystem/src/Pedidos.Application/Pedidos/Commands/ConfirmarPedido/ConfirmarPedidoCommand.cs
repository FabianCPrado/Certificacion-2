using MediatR;
using Pedidos.Application.Exceptions;
using Pedidos.Application.Interfaces.Pedidos;
using Pedidos.Domain.Pedidos;
using Pedidos.Application.Common;

namespace Pedidos.Application.Pedidos.Commands.ConfirmarPedido;

public sealed record ConfirmarPedidoCommand(Guid PedidoId) : ICommand;

public sealed class ConfirmarPedidoCommandHandler(IPedidoRepository pedidos)
    : IRequestHandler<ConfirmarPedidoCommand>
{
    public async Task Handle(ConfirmarPedidoCommand request, CancellationToken cancellationToken)
    {
        var pedido = await pedidos.ObtenerPorIdAsync(request.PedidoId, cancellationToken)
            ?? throw new NotFoundException($"No existe el pedido {request.PedidoId}.");

        await pedidos.ConfirmarAsync(pedido, cancellationToken);
    }
}
