using MediatR;
using Pedidos.Application.Common;
using Pedidos.Application.Interfaces.Productos;
using Pedidos.Application.Productos.Queries.ObtenerProducto;

namespace Pedidos.Application.Productos.Commands.CrearProducto;

/// <summary>Datos para crear un producto en el Catálogo. Devuelve el producto creado.</summary>
public sealed record CrearProductoCommand(string Nombre, string Categoria, decimal Precio, int Stock) : ICommand<ProductoDto>;

public sealed class CrearProductoCommandHandler(ICatalogoProductos catalogo)
    : IRequestHandler<CrearProductoCommand, ProductoDto>
{
    public Task<ProductoDto> Handle(CrearProductoCommand request, CancellationToken cancellationToken) =>
        catalogo.CrearAsync(request.Categoria, request.Nombre, request.Precio, request.Stock, cancellationToken);
}
