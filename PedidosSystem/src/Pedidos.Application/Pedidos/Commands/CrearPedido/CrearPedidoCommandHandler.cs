using MediatR;
using Pedidos.Application.Interfaces.Pedidos;

namespace Pedidos.Application.Pedidos.Commands.CrearPedido;

public sealed class CrearPedidoCommandHandler(
    IPedidoRepository pedidos,
    TimeProvider tiempo) : IRequestHandler<CrearPedidoCommand, Guid>
{
    public async Task<Guid> Handle(CrearPedidoCommand request, CancellationToken cancellationToken)
    {
        // El repositorio valida las reglas, arma el pedido con sus líneas y lo persiste.
        var pedido = await pedidos.CrearAsync(
            request.ClienteId, request.ToEmail(), request.ToDireccion(), request.Moneda,
            tiempo.GetUtcNow().UtcDateTime, request.ToLineasNuevas(), cancellationToken);

        return pedido.Id;
    }
}
