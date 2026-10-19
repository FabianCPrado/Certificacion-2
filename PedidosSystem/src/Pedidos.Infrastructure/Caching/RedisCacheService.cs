using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Pedidos.Application.Interfaces.Common;

namespace Pedidos.Infrastructure.Caching;

/// <summary>
/// Implementa <see cref="ICacheService"/> sobre Redis (vía IDistributedCache), guardando los valores como JSON.
/// El caché es una optimización: si Redis no responde, se registra una advertencia y la aplicación sigue con la base de datos.
/// </summary>
public sealed class RedisCacheService(IDistributedCache cache, ILogger<RedisCacheService> logger) : ICacheService
{
    public async Task<T?> ObtenerAsync<T>(string clave, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = await cache.GetStringAsync(clave, cancellationToken);
            return json is null ? default : JsonSerializer.Deserialize<T>(json);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Para quien llama, un fallo de Redis es igual a "no está en el caché": consulta la base
            logger.LogWarning(ex, "Redis no disponible al leer la clave {Clave}; se usa la base de datos.", clave);
            return default;
        }
    }

    public async Task GuardarAsync<T>(string clave, T valor, TimeSpan duracion, CancellationToken cancellationToken = default)
    {
        try
        {
            await cache.SetStringAsync(
                clave,
                JsonSerializer.Serialize(valor),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = duracion },
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Redis no disponible al guardar la clave {Clave}; la respuesta sale sin caché.", clave);
        }
    }

    public async Task EliminarAsync(string clave, CancellationToken cancellationToken = default)
    {
        try
        {
            await cache.RemoveAsync(clave, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Redis no disponible al eliminar la clave {Clave}.", clave);
        }
    }
}
