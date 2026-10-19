using Pedidos.Application.Productos.Queries.ObtenerProducto;

namespace Pedidos.Application.Interfaces.Demo;

/// <summary>El producto leído, de dónde salió ("cache" o "base") y cuánto tardó.</summary>
public sealed record LecturaCacheDto(string Origen, long Ms, ProductoDto Producto);

/// <summary>
/// Demo de la Clase 5 (Redis): la versión "mala" y la "buena" de cache-aside para comparar.
/// Application solo ve esta interfaz; la implementación con IDistributedCache vive en Infrastructure.
/// </summary>
public interface IDemoCache
{
    /// <summary>MALO: guarda en caché sin expiración (la clave queda para siempre).</summary>
    Task<LecturaCacheDto?> ObtenerMaloAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>MALO: cambia el precio en la base y NO toca la caché, que sigue con el precio viejo. Falso si no existe.</summary>
    Task<bool> ActualizarPrecioMaloAsync(Guid id, decimal precio, CancellationToken cancellationToken = default);

    /// <summary>BUENO: cache-aside con expiración de 10 minutos.</summary>
    Task<LecturaCacheDto?> ObtenerBuenoAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>BUENO: cambia el precio en la base e invalida la clave. Falso si no existe.</summary>
    Task<bool> ActualizarPrecioBuenoAsync(Guid id, decimal precio, CancellationToken cancellationToken = default);
}
