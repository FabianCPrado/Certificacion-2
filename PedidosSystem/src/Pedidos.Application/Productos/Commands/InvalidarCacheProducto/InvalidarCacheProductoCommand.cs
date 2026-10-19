using MediatR;
using Pedidos.Application.Common;
using Pedidos.Application.Interfaces.Common;

namespace Pedidos.Application.Productos.Commands.InvalidarCacheProducto;

public sealed record InvalidarCacheProductoCommand(string Categoria, string ProductoId) : ICommand;

/// <summary>Saca el producto del caché: la próxima lectura vuelve al servicio Catálogo.</summary>
public sealed class InvalidarCacheProductoCommandHandler(ICacheService cache)
    : IRequestHandler<InvalidarCacheProductoCommand>
{
    public Task Handle(InvalidarCacheProductoCommand request, CancellationToken cancellationToken) =>
        cache.EliminarAsync(ClavesCache.Producto(request.Categoria, request.ProductoId), cancellationToken);
}
