using System.Diagnostics;
using Pedidos.Application.Common;
using Pedidos.Application.Exceptions;
using Pedidos.Application.Interfaces.Common;
using Pedidos.Application.Interfaces.Productos;
using Pedidos.Application.Productos.Queries.ObtenerProducto;
using MediatR;

namespace Pedidos.Application.Productos.Queries.ObtenerProductoCacheado;

public sealed record ObtenerProductoCacheadoQuery(Guid ProductoId) : IQuery<ProductoCacheadoDto>;

/// <summary>El producto, de dónde salió ("redis" o "sql") y cuánto tardó: para mostrar el caché en clase.</summary>
public sealed record ProductoCacheadoDto(ProductoDto Producto, string Origen, double Milisegundos);

/// <summary>Cache-aside: primero Redis; si no está, SQL, y el resultado se guarda para la próxima vez.</summary>
public sealed class ObtenerProductoCacheadoQueryHandler(IProductoConsultas consultas, ICacheService cache)
    : IRequestHandler<ObtenerProductoCacheadoQuery, ProductoCacheadoDto>
{
    private static readonly TimeSpan Duracion = TimeSpan.FromMinutes(5);

    public async Task<ProductoCacheadoDto> Handle(ObtenerProductoCacheadoQuery request, CancellationToken cancellationToken)
    {
        var reloj = Stopwatch.StartNew();
        var clave = ClavesCache.Producto(request.ProductoId);

        var enCache = await cache.ObtenerAsync<ProductoDto>(clave, cancellationToken);
        if (enCache is not null)
            return new ProductoCacheadoDto(enCache, "redis", reloj.Elapsed.TotalMilliseconds);

        var producto = await consultas.ObtenerPorIdAsync(request.ProductoId, cancellationToken)
            ?? throw new NotFoundException($"No existe el producto {request.ProductoId}.");

        await cache.GuardarAsync(clave, producto, Duracion, cancellationToken);
        return new ProductoCacheadoDto(producto, "sql", reloj.Elapsed.TotalMilliseconds);
    }
}
