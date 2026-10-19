using MediatR;
using Pedidos.Application.Common;
using Pedidos.Application.Exceptions;
using Pedidos.Application.Interfaces.Pedidos;

namespace Pedidos.Application.Pedidos.Queries.ObtenerPedido;

public sealed record ObtenerPedidoQuery(Guid PedidoId) : IQuery<PedidoDto>;

/// <summary>Consulta: solo usa <see cref="IPedidosConsultas"/> (lado de lectura), nunca el repositorio de escritura.</summary>
public sealed class ObtenerPedidoQueryHandler(IPedidosConsultas consultas)
    : IRequestHandler<ObtenerPedidoQuery, PedidoDto>
{
    public async Task<PedidoDto> Handle(ObtenerPedidoQuery request, CancellationToken cancellationToken) =>
        await consultas.ObtenerPorIdAsync(request.PedidoId, cancellationToken)
            ?? throw new NotFoundException($"No existe el pedido {request.PedidoId}.");
}
