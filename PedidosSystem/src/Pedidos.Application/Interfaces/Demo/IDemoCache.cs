using Pedidos.Application.Productos.Queries.ObtenerProducto;

namespace Pedidos.Application.Interfaces.Demo;

/// <summary>El producto leído, de dónde salió ("cache" o "catalogo") y cuánto tardó.</summary>
public sealed record LecturaCacheDto(string Origen, long Ms, ProductoDto Producto);

/// <summary>
/// Demo de la Clase 5 (Redis): la versión "mala" y la "buena" de cache-aside para comparar.
/// Application solo ve esta interfaz; la implementación con IDistributedCache vive en Infrastructure.
/// </summary>
public interface IDemoCache
{
    /// <summary>MALO: guarda en caché sin expiración (la clave queda para siempre).</summary>
    Task<LecturaCacheDto?> ObtenerMaloAsync(string categoria, string id, CancellationToken cancellationToken = default);

    /// <summary>MALO: cambia el precio en el Catálogo y NO toca la caché, que sigue con el precio viejo. Falso si no existe.</summary>
    Task<bool> ActualizarPrecioMaloAsync(string categoria, string id, decimal precio, CancellationToken cancellationToken = default);

    /// <summary>BUENO: cache-aside con expiración de 10 minutos.</summary>
    Task<LecturaCacheDto?> ObtenerBuenoAsync(string categoria, string id, CancellationToken cancellationToken = default);

    /// <summary>BUENO: cambia el precio en el Catálogo e invalida la clave. Falso si no existe.</summary>
    Task<bool> ActualizarPrecioBuenoAsync(string categoria, string id, decimal precio, CancellationToken cancellationToken = default);
}
