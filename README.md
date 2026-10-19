# Certificación 2 — Máster Profesional en Microservicios con C# y .NET

Proyecto transversal del curso (LT Tech, .NET 10). Son **dos microservicios independientes**, cada uno con su propia solución y su propia base de datos:

| Carpeta | Servicio | Base de datos | Puertos (https / http) |
|---|---|---|---|
| [`PedidosSystem/`](PedidosSystem) | Pedidos, productos, auditoría, caché, imágenes y Cosmos | SQL Server LocalDB `PedidosDb` (+ Redis, Mongo, Azurite, Cosmos) | 7101 / 5101 |
| [`ClientesSystem/`](ClientesSystem) | Clientes | SQL Server LocalDB `ClientesDb` | 7201 / 5201 |

Los dos usan **Clean Architecture** (`Domain` → `Application` → `Infrastructure` → `Api`), **CQRS con MediatR 12.5.0**, FluentValidation y un `GlobalExceptionHandler` que responde con `ProblemDetails`. Cada servicio tiene su propio README con el detalle: [PedidosSystem/README.md](PedidosSystem/README.md) y [ClientesSystem/README.md](ClientesSystem/README.md).

## Cómo se hablan los servicios

```
Clientes (5201)  ──REST──►  Pedidos (5101)  ──►  SQL Server · Redis · Mongo · Azurite · Cosmos
```

- **Pedidos** guarda `ClienteId` como un campo, sin clave foránea. Los datos del cliente los pide a Clientes por REST.
- **Clientes** pregunta a Pedidos por REST antes de eliminar un cliente (si tiene pedidos → 409; si Pedidos no responde → 503).
- Redis, Blob Storage, Cosmos y Mongo viven **solo en Pedidos**, y solo en `Infrastructure`: `Application` conoce únicamente interfaces. Clientes no tiene código de esas tecnologías; su apartado `api/redis/pedidos` solo llama a Pedidos.

## Cómo correrlo

> Primera vez: la guía paso a paso para instalar Docker Desktop y los servicios está en [PedidosSystem/docs/INSTALACION-DOCKER.md](PedidosSystem/docs/INSTALACION-DOCKER.md).

```powershell
# 1. Contenedores (Redis, Redis Insight, Mongo, Mongo Express, Azurite, emulador de Cosmos)
cd PedidosSystem
docker compose up -d

# 2. Bases de datos: ver PedidosSystem/scripts y ClientesSystem/scripts (LocalDB)

# 3. Pedidos
dotnet run --project src/Pedidos.Api --launch-profile https      # https://localhost:7101/swagger

# 4. Clientes (en otra terminal)
cd ..\ClientesSystem
dotnet run --project src/Clientes.Api --launch-profile https     # https://localhost:7201/swagger
```

Si Visual Studio tiene la API corriendo, bloquea los DLL: hay que detenerla antes de compilar desde la terminal.

| Herramienta | Dirección |
|---|---|
| Redis Insight (host `redis`, puerto 6379) | http://localhost:5540 |
| Mongo Express (base `PedidosDb`, colección `auditoria`) | http://localhost:8081 |
| Cosmos Explorer (emulador) | http://localhost:1234 |
| Azurite (Blob) | puerto 10000 |

---

## Lo visto por clases

| Clase | Tema | Qué se aplicó en el proyecto | Dónde está |
|---|---|---|---|
| 1 | Calidad de código, control de versiones y reutilización | SonarAnalyzer activo y `TreatWarningsAsErrors`: una advertencia del analizador (por ejemplo, un controlador con varias responsabilidades) rompe la compilación. Git y GitHub. | `Directory.Build.props`, este repositorio |
| 2 | Patrones de diseño e inyección de dependencias | Decorator en los *behaviors* de MediatR; inyección de dependencias con lifetimes (singleton, scoped, transient); cada interfaz se registra con su implementación en un solo lugar | `Pedidos.Application/Behaviors`, `InfrastructureServiceExtensions.cs` |
| 3 | DDD y arquitecturas (capas, Onion, CQRS) | Entidades y objetos de valor (`Pedido`, `Dinero`, `Email`, `Direccion`), reglas de negocio en el dominio, CQRS (comandos con repositorio, consultas con `IXConsultas`) verificado con tests de arquitectura | `Pedidos.Domain`, `Pedidos.Application`, `CqrsArquitecturaTests.cs` |
| 4 | EF Core, Dapper y capa de datos | `AppDbContext`, mapeo con Fluent API, migraciones, proyección a DTO con `AsNoTracking`, transacciones, procedimientos almacenados, Dapper, demos «malo contra bueno» | `Pedidos.Infrastructure/Persistence`, `scripts/`, `docs/clase4-mapa-de-la-solucion.md` |
| 5 | Almacenamiento en la nube: SQL vs NoSQL, Redis, Blob, Cosmos | Redis (cache-aside), Blob Storage con Azurite, Cosmos con el SDK y con EF Core, y MongoDB para la auditoría | ver «Dónde encontrar cada cosa» |
| 6 | Minimal APIs, OpenAPI/Swagger y gRPC | Swagger en las dos APIs y *minimal APIs* en los endpoints de demo (`/demo/...`). **gRPC no está implementado.** | `Pedidos.Api/Endpoints` |
| 7 | Pruebas e IA en .NET | Pruebas con xUnit: dominio, aplicación, infraestructura, arquitectura CQRS y clientes REST con respuestas simuladas. **No hay pruebas de integración con Testcontainers ni código de IA.** | `PedidosSystem/tests`, `ClientesSystem/tests` |

---

## Dónde encontrar cada cosa

### Redis (caché)
- **Qué hace:** cache-aside. La primera lectura va a SQL, la siguiente sale de Redis. La respuesta indica `"origen": "sql"` o `"redis"` y los milisegundos.
- **Swagger de Pedidos, apartado «Cache»:** `GET /api/cache/pedidos?desde=&hasta=`, `DELETE /api/cache/pedidos?desde=&hasta=`, `GET /api/cache/productos/{id}`, `DELETE /api/cache/productos/{id}`.
- **Swagger de Clientes, apartado «Redis»:** `GET /api/redis/pedidos?desde=&hasta=` (la misma consulta, hecha a través de Pedidos).
- **Código:** `Pedidos.Api/Controllers/CacheController.cs`, `Pedidos.Infrastructure/Caching/RedisCacheService.cs`, `Pedidos.Application/Pedidos/Queries/ObtenerPedidosPorFechasCacheado`. El registro (`AddStackExchangeRedisCache`) está en `InfrastructureServiceExtensions.cs`.
- **Demo «malo contra bueno»:** `/demo/clase5/cache/malo/...` y `/demo/clase5/cache/bueno/...` (solo Development), en `DemoCache.cs`.
- **Si Redis falla:** `RedisCacheService` registra una advertencia y la API sigue con SQL.

### Blob Storage (imágenes de productos)
- **Swagger de Pedidos, apartado «Blob Storage»:** `POST /api/blob/imagenes/{productoId}` (campo `archivo`, solo `image/*`) y `GET /api/blob/imagenes/{productoId}`.
- **Cómo se guarda:** contenedor `productos` de Azurite, con el nombre `{productoId}/{archivo}`. La base de datos no guarda las imágenes.
- **Código:** `Pedidos.Api/Controllers/BlobController.cs`, `Pedidos.Infrastructure/Storage/BlobImagenesStorage.cs`.
- **Para probar:** fotos en `PedidosSystem/ejemplos/fotos` (licencias en `ATRIBUCIONES.md`) y los Id de productos en `PedidosSystem/ejemplos/ids-productos.md`.

### Cosmos DB
Todo en Swagger de Pedidos, apartado **«Cosmos DB»**:

| Qué | Endpoints | Código |
|---|---|---|
| Valoraciones (SDK) | `POST /api/cosmos/valoraciones`, `GET /api/cosmos/valoraciones/{productoId}` | `ValoracionesController.cs`, `Infrastructure/Cosmos/ValoracionCosmos*.cs` |
| Catálogo con el SDK | `POST /api/cosmos/catalogo/seed`, `GET /api/cosmos/catalogo/{categoria}/{id}` (lectura puntual), `.../bueno`, `.../cruzada`, `.../malo` (vulnerable a propósito, solo responde en Development) | `CosmosController.cs`, `Infrastructure/Cosmos/DemoCatalogo.cs` |
| Catálogo por repositorio | `GET /api/cosmos/catalogo/repositorio/{categoria}` | `CosmosRepositorioController.cs`, `CatalogoRepository.cs` |
| Catálogo con **EF Core** | `POST /api/cosmos/ef/seed`, `GET /api/cosmos/ef/{categoria}` (`Bebidas` o `Electro`) | `CosmosEfController.cs`, `CatalogoDbContext.cs`, `DemoCatalogoEf.cs` |

- `UseCosmos` está en `InfrastructureServiceExtensions.cs`. El `CatalogoDbContext` es aparte del `AppDbContext` porque cada `DbContext` usa un solo proveedor.
- El emulador de Cosmos (vNext) siempre informa 1 RU, así que la comparación de costos solo se ve en Azure real.
- **Ejemplos de JSON:** `PedidosSystem/ejemplos/cosmos-01-...` y `cosmos-02-...` (201), `cosmos-03-...` (400).

### MongoDB (auditoría)
- **Qué hace:** `AuditoriaMiddleware` guarda cada `POST`, `PUT` y `DELETE` (método, ruta, estado, duración, usuario, `TraceId` y cuerpo) en la colección `auditoria`.
- **Endpoint:** `GET /api/auditoria?ruta=&desde=&hasta=&pagina=&tamanio=` (solo lectura).
- **Código:** `Pedidos.Api/Middleware/AuditoriaMiddleware.cs` y `Pedidos.Infrastructure/Mongo/`: `AuditoriaMongoRepository` guarda, `AuditoriaMongoConsultas` lee y `MongoAuditoriaColeccion` maneja la conexión y los índices.
- **No usa `DbContext`:** usa el driver oficial, con `IMongoCollection<AuditoriaDocumento>`.

### SQL Server, EF Core y Dapper
- **Endpoints:** `/api/pedidos` (crear, obtener, confirmar, cancelar), `/api/productos` (CRUD) y `/api/pedidos/{id}/cliente`.
- **Demos de la clase 4** (solo Development): `/demo/clase4/...` (`linq`, `n1`, `sql`, `reporte`, `stock`, `transaccion`, Dapper), cada una en versión `malo` y `bueno`.
- **Scripts:** `PedidosSystem/scripts` (`crear-base-de-datos.sql`, `procedimientos-clase4.sql`, `seed-clase4.sql`, `auditoria.sql`). El seed carga 50.000 productos de prueba. `ClientesSystem/scripts/crear-base-de-datos.sql` crea `ClientesDb`.
- **Mapa de la solución:** `PedidosSystem/docs/clase4-mapa-de-la-solucion.md`.

### Clientes (otro servicio)
- **Endpoints:** `POST/GET/PUT/DELETE /api/clientes` y `GET /api/redis/pedidos`.
- **Código:** `ClientesSystem/src` (`Clientes.Domain`, `Clientes.Application`, `Clientes.Infrastructure`, `Clientes.Api`). La llamada a Pedidos está en `Clientes.Infrastructure/Rest/Pedidos/PedidosRestConsultas.cs`; la URL, en `Servicios:Pedidos`.

### Ejemplos y documentación
| Qué | Dónde |
|---|---|
| JSON para `POST /api/pedidos` (válidos y con errores 400/409) | `PedidosSystem/ejemplos/01-...` a `07-...` |
| Id de productos (con y sin imágenes en Blob) | `PedidosSystem/ejemplos/ids-productos.md` |
| Fotos de ejemplo | `PedidosSystem/ejemplos/fotos` |
| Guía de la clase 5 | `PedidosSystem/INSTRUCCIONES-clase5.md` |
| Bases de datos de la clase 5 | `PedidosSystem/docs/clase5-bases-de-datos.md` |
| Almacenamiento | `PedidosSystem/docs/ALMACENAMIENTO.md` |

## Pruebas

```powershell
cd PedidosSystem ; dotnet test
cd ..\ClientesSystem ; dotnet test
```

Cubren el dominio, los handlers y validadores, las reglas de CQRS (ningún comando usa consultas y ninguna consulta usa un repositorio de escritura) y los clientes REST con respuestas simuladas, sin abrir red ni base de datos.
