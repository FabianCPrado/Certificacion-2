using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Pedidos.Application.Interfaces.Demo;
using Pedidos.Application.Productos.Queries.ObtenerProducto;
using Pedidos.Infrastructure.Persistence;

namespace Pedidos.Infrastructure.Caching;

/// <summary>
/// Demo de la Clase 5 con <see cref="IDistributedCache"/> directo, para mostrar la API de Redis paso a paso.
/// Las claves llevan el prefijo "demo:" para no mezclarse con las del caché de la aplicación (pedidos:v1:...).
/// </summary>
public sealed class DemoCache(IDistributedCache cache, AppDbContext context) : IDemoCache
{
    private static readonly TimeSpan Expiracion = TimeSpan.FromMinutes(10);

    private static string ClaveMala(Guid id) => $"demo:malo:producto:{id}";

    private static string ClaveBuena(Guid id) => $"demo:bueno:producto:{id}";

    public async Task<LecturaCacheDto?> ObtenerMaloAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var reloj = Stopwatch.StartNew();
        var clave = ClaveMala(id);

        var json = await cache.GetStringAsync(clave, cancellationToken);
        if (json is not null)
            return new LecturaCacheDto("cache", reloj.ElapsedMilliseconds, JsonSerializer.Deserialize<ProductoDto>(json)!);

        var producto = await LeerDeLaBaseAsync(id, cancellationToken);
        if (producto is null)
            return null;

        // MALO: sin expiración, la clave queda guardada para siempre (TTL = -1)
        await cache.SetStringAsync(clave, JsonSerializer.Serialize(producto), cancellationToken);
        return new LecturaCacheDto("base", reloj.ElapsedMilliseconds, producto);
    }

    public async Task<bool> ActualizarPrecioMaloAsync(Guid id, decimal precio, CancellationToken cancellationToken = default) =>
        // MALO: actualiza la base y no invalida la caché, que sigue con el precio viejo
        await ActualizarPrecioEnLaBaseAsync(id, precio, cancellationToken);

    public async Task<LecturaCacheDto?> ObtenerBuenoAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var reloj = Stopwatch.StartNew();
        var clave = ClaveBuena(id);

        var json = await cache.GetStringAsync(clave, cancellationToken);   // 1. buscar en la caché
        if (json is not null)
            return new LecturaCacheDto("cache", reloj.ElapsedMilliseconds, JsonSerializer.Deserialize<ProductoDto>(json)!);

        var producto = await LeerDeLaBaseAsync(id, cancellationToken);      // 2. si no está, ir a la base
        if (producto is null)
            return null;

        await cache.SetStringAsync(                                          // 3. guardar con expiración
            clave,
            JsonSerializer.Serialize(producto),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = Expiracion },
            cancellationToken);

        return new LecturaCacheDto("base", reloj.ElapsedMilliseconds, producto);
    }

    public async Task<bool> ActualizarPrecioBuenoAsync(Guid id, decimal precio, CancellationToken cancellationToken = default)
    {
        var existe = await ActualizarPrecioEnLaBaseAsync(id, precio, cancellationToken);
        await cache.RemoveAsync(ClaveBuena(id), cancellationToken);   // BUENO: la próxima lectura va a la base
        return existe;
    }

    private Task<ProductoDto?> LeerDeLaBaseAsync(Guid id, CancellationToken cancellationToken) =>
        context.Productos.AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new ProductoDto(p.Id, p.Nombre, p.Categoria, p.Precio, p.Stock))
            .FirstOrDefaultAsync(cancellationToken);

    private async Task<bool> ActualizarPrecioEnLaBaseAsync(Guid id, decimal precio, CancellationToken cancellationToken) =>
        await context.Productos.Where(p => p.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Precio, precio), cancellationToken) > 0;
}
