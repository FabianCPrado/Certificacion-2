using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Pedidos.Application.Exceptions;
using Pedidos.Application.Interfaces.Demo;
using Pedidos.Application.Interfaces.Productos;
using Pedidos.Application.Productos.Queries.ObtenerProducto;

namespace Pedidos.Infrastructure.Caching;

/// <summary>
/// Demo de la Clase 5 con <see cref="IDistributedCache"/> directo, para mostrar la API de Redis paso a paso.
/// Las claves llevan el prefijo "demo:" para no mezclarse con las del caché de la aplicación (pedidos:v1:...).
/// El producto se lee del servicio Catálogo.
/// </summary>
public sealed class DemoCache(IDistributedCache cache, ICatalogoProductos catalogo) : IDemoCache
{
    private static readonly TimeSpan Expiracion = TimeSpan.FromMinutes(10);

    private static string ClaveMala(string categoria, string id) => $"demo:malo:producto:{categoria}:{id}";

    private static string ClaveBuena(string categoria, string id) => $"demo:bueno:producto:{categoria}:{id}";

    public async Task<LecturaCacheDto?> ObtenerMaloAsync(string categoria, string id, CancellationToken cancellationToken = default)
    {
        var reloj = Stopwatch.StartNew();
        var clave = ClaveMala(categoria, id);

        var json = await cache.GetStringAsync(clave, cancellationToken);
        if (json is not null)
            return new LecturaCacheDto("cache", reloj.ElapsedMilliseconds, JsonSerializer.Deserialize<ProductoDto>(json)!);

        var producto = await catalogo.ObtenerAsync(categoria, id, cancellationToken);
        if (producto is null)
            return null;

        // MALO: sin expiración, la clave queda guardada para siempre (TTL = -1)
        await cache.SetStringAsync(clave, JsonSerializer.Serialize(producto), cancellationToken);
        return new LecturaCacheDto("catalogo", reloj.ElapsedMilliseconds, producto);
    }

    public async Task<bool> ActualizarPrecioMaloAsync(
        string categoria, string id, decimal precio, CancellationToken cancellationToken = default) =>
        // MALO: actualiza el Catálogo y no invalida la caché, que sigue con el precio viejo
        await CambiarPrecioAsync(categoria, id, precio, cancellationToken);

    public async Task<LecturaCacheDto?> ObtenerBuenoAsync(string categoria, string id, CancellationToken cancellationToken = default)
    {
        var reloj = Stopwatch.StartNew();
        var clave = ClaveBuena(categoria, id);

        var json = await cache.GetStringAsync(clave, cancellationToken);   // 1. buscar en la caché
        if (json is not null)
            return new LecturaCacheDto("cache", reloj.ElapsedMilliseconds, JsonSerializer.Deserialize<ProductoDto>(json)!);

        var producto = await catalogo.ObtenerAsync(categoria, id, cancellationToken);   // 2. si no está, ir al Catálogo
        if (producto is null)
            return null;

        await cache.SetStringAsync(                                          // 3. guardar con expiración
            clave,
            JsonSerializer.Serialize(producto),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = Expiracion },
            cancellationToken);

        return new LecturaCacheDto("catalogo", reloj.ElapsedMilliseconds, producto);
    }

    public async Task<bool> ActualizarPrecioBuenoAsync(
        string categoria, string id, decimal precio, CancellationToken cancellationToken = default)
    {
        var existe = await CambiarPrecioAsync(categoria, id, precio, cancellationToken);
        await cache.RemoveAsync(ClaveBuena(categoria, id), cancellationToken);   // BUENO: la próxima lectura va al Catálogo
        return existe;
    }

    private async Task<bool> CambiarPrecioAsync(string categoria, string id, decimal precio, CancellationToken cancellationToken)
    {
        try
        {
            await catalogo.CambiarPrecioAsync(categoria, id, precio, cancellationToken);
            return true;
        }
        catch (NotFoundException)
        {
            return false;
        }
    }
}
