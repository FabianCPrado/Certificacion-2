using MediatR;
using Pedidos.Application.Common;
using Pedidos.Application.Interfaces.Common;

namespace Pedidos.Application.Pedidos.Commands.InvalidarCachePedidos;

public sealed record InvalidarCachePedidosCommand(DateOnly Desde, DateOnly Hasta) : ICommand;

/// <summary>Saca del caché el resultado de ese rango de fechas: la próxima lectura vuelve a SQL.</summary>
public sealed class InvalidarCachePedidosCommandHandler(ICacheService cache)
    : IRequestHandler<InvalidarCachePedidosCommand>
{
    public Task Handle(InvalidarCachePedidosCommand request, CancellationToken cancellationToken) =>
        cache.EliminarAsync(ClavesCache.PedidosPorFechas(request.Desde, request.Hasta), cancellationToken);
}
