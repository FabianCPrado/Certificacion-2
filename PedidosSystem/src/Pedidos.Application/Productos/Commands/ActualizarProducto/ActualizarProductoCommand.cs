using MediatR;
using Pedidos.Application.Common;
using Pedidos.Application.Interfaces.Productos;

namespace Pedidos.Application.Productos.Commands.ActualizarProducto;

/// <summary>La categoría identifica al producto (junto con el id) y no se cambia.</summary>
public sealed record ActualizarProductoCommand(
    string Categoria, string ProductoId, string Nombre, decimal Precio, int Stock) : ICommand;

public sealed class ActualizarProductoCommandHandler(ICatalogoProductos catalogo)
    : IRequestHandler<ActualizarProductoCommand>
{
    public Task Handle(ActualizarProductoCommand request, CancellationToken cancellationToken) =>
        catalogo.ActualizarAsync(
            request.Categoria, request.ProductoId, request.Nombre, request.Precio, request.Stock, cancellationToken);
}
