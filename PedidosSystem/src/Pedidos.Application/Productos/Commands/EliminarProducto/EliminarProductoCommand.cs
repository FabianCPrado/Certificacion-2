using MediatR;
using Pedidos.Application.Common;
using Pedidos.Application.Interfaces.Productos;

namespace Pedidos.Application.Productos.Commands.EliminarProducto;

public sealed record EliminarProductoCommand(string Categoria, string ProductoId) : ICommand;

public sealed class EliminarProductoCommandHandler(ICatalogoProductos catalogo)
    : IRequestHandler<EliminarProductoCommand>
{
    public Task Handle(EliminarProductoCommand request, CancellationToken cancellationToken) =>
        catalogo.EliminarAsync(request.Categoria, request.ProductoId, cancellationToken);
}
