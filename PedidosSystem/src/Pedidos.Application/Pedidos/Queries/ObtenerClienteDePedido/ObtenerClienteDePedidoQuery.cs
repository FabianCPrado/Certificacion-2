using MediatR;
using Pedidos.Application.Clientes.Queries.ObtenerCliente;
using Pedidos.Application.Common;
using Pedidos.Application.Exceptions;
using Pedidos.Application.Interfaces.Clientes;
using Pedidos.Application.Interfaces.Pedidos;

namespace Pedidos.Application.Pedidos.Queries.ObtenerClienteDePedido;

/// <summary>Datos del cliente de un pedido. El pedido solo guarda el ClienteId: los datos los tiene el servicio Clientes.</summary>
public sealed record ObtenerClienteDePedidoQuery(Guid PedidoId) : IQuery<ClienteDto>;

/// <summary>Consulta: solo usa interfaces de lectura (<see cref="IPedidosConsultas"/> e <see cref="IClienteConsultas"/>).</summary>
public sealed class ObtenerClienteDePedidoQueryHandler(IPedidosConsultas pedidos, IClienteConsultas clientes)
    : IRequestHandler<ObtenerClienteDePedidoQuery, ClienteDto>
{
    public async Task<ClienteDto> Handle(ObtenerClienteDePedidoQuery request, CancellationToken cancellationToken)
    {
        var pedido = await pedidos.ObtenerPorIdAsync(request.PedidoId, cancellationToken)
            ?? throw new NotFoundException($"No existe el pedido {request.PedidoId}.");

        return await clientes.ObtenerPorIdAsync(pedido.ClienteId, cancellationToken)
            ?? throw new NotFoundException(
                $"El pedido {request.PedidoId} apunta al cliente {pedido.ClienteId}, que no existe en el servicio Clientes.");
    }
}
