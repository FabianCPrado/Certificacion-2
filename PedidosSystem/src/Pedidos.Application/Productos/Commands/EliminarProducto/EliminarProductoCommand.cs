using MediatR;
using Pedidos.Application.Exceptions;
using Pedidos.Application.Interfaces.Productos;
using Pedidos.Application.Common;

namespace Pedidos.Application.Productos.Commands.EliminarProducto;

public sealed record EliminarProductoCommand(Guid ProductoId) : ICommand;

public sealed class EliminarProductoCommandHandler(IProductoRepository productos)
    : IRequestHandler<EliminarProductoCommand>
{
    public async Task Handle(EliminarProductoCommand request, CancellationToken cancellationToken)
    {
        var producto = await productos.ObtenerPorIdAsync(request.ProductoId, cancellationToken)
            ?? throw new NotFoundException($"No existe el producto {request.ProductoId}.");

        await productos.EliminarAsync(producto, cancellationToken);
    }
}
