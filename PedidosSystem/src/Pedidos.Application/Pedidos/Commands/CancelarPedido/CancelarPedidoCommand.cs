using MediatR;
using Pedidos.Application.Exceptions;
using Pedidos.Application.Interfaces.Pedidos;
using Pedidos.Domain.Pedidos;
using Pedidos.Application.Common;

namespace Pedidos.Application.Pedidos.Commands.CancelarPedido;

public sealed record CancelarPedidoCommand(Guid PedidoId) : ICommand;

public sealed class CancelarPedidoCommandHandler(IPedidoRepository pedidos)
    : IRequestHandler<CancelarPedidoCommand>
{
    public async Task Handle(CancelarPedidoCommand request, CancellationToken cancellationToken)
    {
        var pedido = await pedidos.ObtenerPorIdAsync(request.PedidoId, cancellationToken)
            ?? throw new NotFoundException($"No existe el pedido {request.PedidoId}.");

        await pedidos.CancelarAsync(pedido, cancellationToken);
    }
}
