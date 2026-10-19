using MediatR;
using Pedidos.Application.Interfaces.Productos;
using Pedidos.Application.Common;

namespace Pedidos.Application.Productos.Commands.CrearProducto;

/// <summary>Datos para crear un producto. Devuelve el Id del producto creado.</summary>
public sealed record CrearProductoCommand(string Nombre, string Categoria, decimal Precio, int Stock) : ICommand<Guid>;

public sealed class CrearProductoCommandHandler(IProductoRepository productos)
    : IRequestHandler<CrearProductoCommand, Guid>
{
    public async Task<Guid> Handle(CrearProductoCommand request, CancellationToken cancellationToken)
    {
        var producto = await productos.CrearAsync(
            request.Nombre, request.Categoria, request.Precio, request.Stock, cancellationToken);

        return producto.Id;
    }
}
