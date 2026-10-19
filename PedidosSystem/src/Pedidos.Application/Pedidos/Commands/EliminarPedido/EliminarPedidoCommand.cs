using MediatR;
using Pedidos.Application.Exceptions;
using Pedidos.Application.Interfaces.Pedidos;
using Pedidos.Application.Common;

namespace Pedidos.Application.Pedidos.Commands.EliminarPedido;

public sealed record EliminarPedidoCommand(Guid PedidoId) : ICommand;

public sealed class EliminarPedidoCommandHandler(IPedidoRepository pedidos)
    : IRequestHandler<EliminarPedidoCommand>
{
    public async Task Handle(EliminarPedidoCommand request, CancellationToken cancellationToken)
    {
        var pedido = await pedidos.ObtenerPorIdAsync(request.PedidoId, cancellationToken)
            ?? throw new NotFoundException($"No existe el pedido {request.PedidoId}.");

        await pedidos.EliminarAsync(pedido, cancellationToken);
    }
}
