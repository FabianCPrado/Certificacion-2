# Tecnologías de almacenamiento · configuración

Un documento por tecnología, con la misma estructura: para qué se usa, configuración (Docker, cadena de conexión, paquetes), registro en el código, dónde está el código, cómo verlo y problemas frecuentes.

| Tecnología | Para qué | Documento |
|---|---|---|
| SQL Server (LocalDB) | Pedidos, líneas, productos y clientes | [sql-server.md](sql-server.md) |
| Redis | Caché de lecturas (cache-aside) | [redis.md](redis.md) |
| MongoDB | Auditoría de POST, PUT y DELETE | [mongodb.md](mongodb.md) |
| Cosmos DB (emulador) | Catálogo (SDK y EF Core) y valoraciones | [cosmos-db.md](cosmos-db.md) |
| Blob Storage (Azurite) | Imágenes de productos | [blob-storage-azurite.md](blob-storage-azurite.md) |

Para la visión de conjunto y qué mostrar en clase: [`clase5-bases-de-datos.md`](../clase5-bases-de-datos.md). Para la elección de almacenamiento de cada servicio: [`ALMACENAMIENTO.md`](../ALMACENAMIENTO.md).

## Puertos y cadenas de conexión

| Servicio | Puerto | Clave en `ConnectionStrings` | Interfaz |
|---|---|---|---|
| SQL Server LocalDB | (local) | `Pedidos` | SSMS / Azure Data Studio |
| Redis | 6379 | `Redis` | Redis Insight `http://localhost:5540` |
| MongoDB | 27017 | `Mongo` | Mongo Express `http://localhost:8081` |
| Cosmos DB (emulador) | 8083 (HTTPS) | `Cosmos` | Data Explorer `http://localhost:1234` |
| Azurite (Blob) | 10000 | `Storage` | VS Code o Azure Storage Explorer |
| API | 5101 / 7101 | – | Swagger `https://localhost:7101/swagger` |

Las cadenas están en [`appsettings.Development.json`](../../src/Pedidos.Api/appsettings.Development.json). Todas, salvo `Pedidos`, tienen un valor por defecto en el código para el emulador local.

## Levantar todo
```powershell
docker compose up -d        # Redis, Redis Insight, MongoDB, Mongo Express, Azurite y Cosmos
```
Todos los servicios se declaran en el único [`docker-compose.yml`](../../docker-compose.yml). SQL Server LocalDB no usa Docker.

## Reglas que se repiten en todas

1. **Todo el código de cada tecnología vive en `Pedidos.Infrastructure`.** Application solo define interfaces y Domain no referencia a ninguna base.
2. **Los clientes son Singleton** (`CosmosClient`, `BlobServiceClient`, cliente de MongoDB, conexión a Redis): uno por aplicación. Solo el `DbContext` de EF Core es Scoped.
3. **Las claves y contraseñas reales no van en el repositorio.** Las de los emuladores (Cosmos y Azurite) son públicas y fijas, no secretos. Con servicios reales, usar *user secrets* o variables de entorno.
4. **Cambiar de tecnología es cambiar el registro** en [`InfrastructureServiceExtensions.cs`](../../src/Pedidos.Infrastructure/InfrastructureServiceExtensions.cs), sin tocar handlers ni controladores.
