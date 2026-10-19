# Redis · caché

## Para qué se usa
Caché de lecturas con **cache-aside**: se busca en Redis; si no está, se lee de SQL Server y el resultado se guarda con expiración. También hay una demo "malo contra bueno" de la Clase 5.

## Resumen de la configuración

| Dato | Valor |
|---|---|
| Imagen Docker | `redis:7-alpine` |
| Puerto | `6379` |
| Volumen | ninguno (las claves viven dentro del contenedor y son solo caché) |
| Contraseña | ninguna (solo local) |
| Paquete NuGet | `Microsoft.Extensions.Caching.StackExchangeRedis` 10.0.12 |
| Clave en `appsettings.Development.json` | `ConnectionStrings:Redis` |
| Visor | Redis Insight, `http://localhost:5540` |

## Docker (`docker-compose.yml`)
```yaml
redis:
  image: redis:7-alpine
  ports:
    - "6379:6379"

redisinsight:
  image: redis/redisinsight:latest
  ports:
    - "5540:5540"
  depends_on:
    - redis
```

## Cadena de conexión
```json
"Redis": "localhost:6379,abortConnect=false,connectTimeout=1000,syncTimeout=1000,asyncTimeout=1000"
```

| Parte | Qué hace |
|---|---|
| `localhost:6379` | Dirección del servidor |
| `abortConnect=false` | Si Redis está caído al arrancar, la aplicación arranca igual y reintenta |
| `connectTimeout=1000` | Espera máxima para conectar: 1 segundo |
| `syncTimeout` / `asyncTimeout` | Espera máxima por operación: 1 segundo |

Los timeouts cortos hacen que, con Redis caído, la API siga con SQL sin quedarse esperando. Aun así tarda unos 4 segundos por consulta (lee y guarda, y cada intento espera su timeout).

## Registro en el código
[`InfrastructureServiceExtensions.cs`](../../src/Pedidos.Infrastructure/InfrastructureServiceExtensions.cs)
```csharp
services.AddStackExchangeRedisCache(options =>
    options.Configuration = configuration.GetConnectionString("Redis")
        ?? "localhost:6379,abortConnect=false,connectTimeout=1000,syncTimeout=1000,asyncTimeout=1000");
services.AddSingleton<ICacheService, RedisCacheService>();
```
No se usa `InstanceName`: las claves se escriben completas.

## Dónde está el código

| Qué | Dónde |
|---|---|
| Interfaz (Application) | [`ICacheService`](../../src/Pedidos.Application/Interfaces/Common/ICacheService.cs) |
| Implementación | [`RedisCacheService`](../../src/Pedidos.Infrastructure/Caching/RedisCacheService.cs) sobre `IDistributedCache` |
| Claves | [`ClavesCache`](../../src/Pedidos.Application/Common/ClavesCache.cs) |
| Demo malo/bueno | [`DemoCache`](../../src/Pedidos.Infrastructure/Caching/DemoCache.cs) |
| Handlers que lo usan | `ObtenerProductoCacheadoQuery`, `ObtenerPedidosPorFechasCacheadoQuery`, `InvalidarCache*Command` |

## Claves y expiración

| Clave | Expiración |
|---|---|
| `pedidos:v1:producto:{id}` | 5 minutos |
| `pedidos:v1:pedidos-por-fechas:{yyyyMMdd}:{yyyyMMdd}` | 5 minutos |
| `demo:bueno:producto:{id}` | 10 minutos (se invalida al actualizar el precio) |
| `demo:malo:producto:{id}` | **sin expiración** (`TTL` = -1, a propósito) |

La clave es un texto armado con los parámetros, no un hash. Un rango de fechas distinto genera otra clave y vuelve a SQL.

## Endpoints

| Método | Ruta |
|---|---|
| GET / DELETE | `/api/cache/productos/{id}` |
| GET / DELETE | `/api/cache/pedidos?desde=2026-09-15&hasta=2026-10-14` |
| GET / PUT | `/demo/clase5/cache/malo/{id}` y `/cache/malo/{id}/precio/{precio}` |
| GET / PUT | `/demo/clase5/cache/bueno/{id}` y `/cache/bueno/{id}/precio/{precio}` |

## Cómo verlo
- **Redis Insight:** `http://localhost:5540` → *Add Redis database* → host **`redis`**, puerto `6379`. Usar `redis` y no `localhost`: Insight corre en su propio contenedor.
- **Consola:** `docker exec -it pedidossystem-redis-1 redis-cli` y luego `KEYS *`, `TTL <clave>`, `GET <clave>`.

## Si Redis cae
`RedisCacheService` captura el error, escribe una advertencia en el log y la consulta sigue en SQL Server. La respuesta es 200, más lenta.

## Problemas frecuentes

| Síntoma | Causa y solución |
|---|---|
| Insight no conecta con `127.0.0.1` | Dentro del contenedor, `127.0.0.1` es Insight mismo. Usar el host `redis` |
| Un producto editado se ve viejo | El `PUT` de `/api/productos` no invalida el caché; esperar 5 minutos o usar el `DELETE` de `/api/cache/...` |
| No aparecen claves | Vencieron (5 o 10 minutos) o no se hizo ninguna lectura; hacer un `GET` y refrescar |
| Error de paquete `System.Security.Cryptography.Pkcs` | El paquete de Redis trae una versión vulnerable; se fija la 10.0.13 en `Directory.Packages.props` (`CentralPackageTransitivePinningEnabled`) |

## Pasar a Azure Cache for Redis
Cambiar solo la cadena de conexión, guardada en *user secrets* o variables de entorno:
```
<nombre>.redis.cache.windows.net:6380,password=<clave>,ssl=True,abortConnect=False
```
El código no cambia.
