using MediatR;
using Pedidos.Application.Common;
using Pedidos.Application.Exceptions;
using Pedidos.Application.Interfaces.Productos;

namespace Pedidos.Application.Productos.Queries.ObtenerProducto;

public sealed record ObtenerProductoQuery(Guid ProductoId) : IQuery<ProductoDto>;

/// <summary>Consulta: solo usa <see cref="IProductoConsultas"/> (lado de lectura), nunca el repositorio de escritura.</summary>
public sealed class ObtenerProductoQueryHandler(IProductoConsultas consultas)
    : IRequestHandler<ObtenerProductoQuery, ProductoDto>
{
    public async Task<ProductoDto> Handle(ObtenerProductoQuery request, CancellationToken cancellationToken) =>
        await consultas.ObtenerPorIdAsync(request.ProductoId, cancellationToken)
            ?? throw new NotFoundException($"No existe el producto {request.ProductoId}.");
}
