using MediatR;
using Pedidos.Application.Interfaces.Pedidos;
using Pedidos.Application.Interfaces.Productos;

namespace Pedidos.Application.Pedidos.Commands.CrearPedido;

public sealed class CrearPedidoCommandHandler(
    IPedidoRepository pedidos,
    ICatalogoProductos catalogo,
    TimeProvider tiempo) : IRequestHandler<CrearPedidoCommand, Guid>
{
    public async Task<Guid> Handle(CrearPedidoCommand request, CancellationToken cancellationToken)
    {
        // Nombre y precio salen del servicio Catálogo; el repositorio valida las reglas, arma el pedido y lo persiste.
        var lineas = await catalogo.ResolverLineasAsync(request, cancellationToken);

        var pedido = await pedidos.CrearAsync(
            request.ClienteId, request.ToEmail(), request.ToDireccion(), request.Moneda,
            tiempo.GetUtcNow().UtcDateTime, lineas, cancellationToken);

        return pedido.Id;
    }
}
