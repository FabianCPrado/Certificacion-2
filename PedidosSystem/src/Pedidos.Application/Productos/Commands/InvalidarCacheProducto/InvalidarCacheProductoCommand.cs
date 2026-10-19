using MediatR;
using Pedidos.Application.Common;
using Pedidos.Application.Interfaces.Common;

namespace Pedidos.Application.Productos.Commands.InvalidarCacheProducto;

public sealed record InvalidarCacheProductoCommand(Guid ProductoId) : ICommand;

/// <summary>Saca el producto del caché: la próxima lectura vuelve a SQL.</summary>
public sealed class InvalidarCacheProductoCommandHandler(ICacheService cache)
    : IRequestHandler<InvalidarCacheProductoCommand>
{
    public Task Handle(InvalidarCacheProductoCommand request, CancellationToken cancellationToken) =>
        cache.EliminarAsync(ClavesCache.Producto(request.ProductoId), cancellationToken);
}
