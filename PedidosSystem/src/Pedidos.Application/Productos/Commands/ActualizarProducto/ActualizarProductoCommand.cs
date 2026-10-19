using MediatR;
using Pedidos.Application.Exceptions;
using Pedidos.Application.Interfaces.Productos;
using Pedidos.Application.Common;

namespace Pedidos.Application.Productos.Commands.ActualizarProducto;

public sealed record ActualizarProductoCommand(
    Guid ProductoId, string Nombre, string Categoria, decimal Precio, int Stock) : ICommand;

public sealed class ActualizarProductoCommandHandler(IProductoRepository productos)
    : IRequestHandler<ActualizarProductoCommand>
{
    public async Task Handle(ActualizarProductoCommand request, CancellationToken cancellationToken)
    {
        var producto = await productos.ObtenerPorIdAsync(request.ProductoId, cancellationToken)
            ?? throw new NotFoundException($"No existe el producto {request.ProductoId}.");

        await productos.ActualizarAsync(
            producto, request.Nombre, request.Categoria, request.Precio, request.Stock, cancellationToken);
    }
}
