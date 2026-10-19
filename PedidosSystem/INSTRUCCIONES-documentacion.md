# Instrucciones para el proyecto generador de documentación / PPTs

Este documento describe **cómo está construido PedidosSystem** (estructura, carpetas e inyección de dependencias) para que el otro proyecto genere la documentación y las presentaciones sin tener que adivinar. Fuente de verdad: el código en `src/`. Si el README contradice al código, manda el código.

---

## 1. Qué es

Backend del servicio **Pedidos** de la *Certificación 2: Desarrollo Avanzado de Microservicios con C# y .NET* (LT Tech).
- .NET 10, **Clean Architecture**, **CQRS con MediatR 12.5.0**, FluentValidation, EF Core + Dapper.
- Documentado con OpenAPI + Swagger UI (solo en Development).
- Solución: `PedidosSystem.slnx`. Paquetes centralizados en `Directory.Packages.props`.

## 2. Capas y regla de dependencias

Las dependencias apuntan **hacia el centro**.

| Proyecto | Responsabilidad | Depende de |
|---|---|---|
| `Pedidos.Domain` | Entidades con reglas de negocio, objetos de valor | — |
| `Pedidos.Application` | Casos de uso (CQRS), validadores, behaviors, **solo interfaces** de lo externo | Domain |
| `Pedidos.Infrastructure` | **Toda** la implementación técnica (SQL Server, Redis, MongoDB, Cosmos, Blob) | Application |
| `Pedidos.Api` | Controladores, middleware, manejo de errores, composición (`Program.cs`) | Application, Infrastructure |

Regla clave: **Redis, MongoDB, Cosmos y Blob se implementan solo en Infrastructure; Application únicamente define interfaces.**

## 3. Estructura de carpetas

```
PedidosSystem/
├── src/
│   ├── Pedidos.Domain/
│   │   ├── Common/          Entity, ReglaNegocioException
│   │   ├── Pedidos/         Pedido, LineaPedido, EstadoPedido
│   │   ├── Productos/       Producto
│   │   ├── Catalogo/        ProductoCatalogo (Cosmos)
│   │   ├── Auditoria/       RegistroAuditoria
│   │   └── ValueObjects/    Dinero, Email, Direccion
│   │
│   ├── Pedidos.Application/
│   │   ├── Behaviors/       LoggingBehavior, ValidationBehavior
│   │   ├── Common/          ClavesCache, Mensajes, PagedResult
│   │   ├── Exceptions/      NotFoundException
│   │   ├── Interfaces/      una carpeta por área: Auditoria, Catalogo,
│   │   │                    Common (IAppDbContext, ICacheService), Demo, Pedidos,
│   │   │                    Productos, Storage, Valoraciones
│   │   ├── Pedidos|Productos|Valoraciones|Auditoria/
│   │   │   ├── Commands/<CasoDeUso>/   Command + Handler + Validator
│   │   │   └── Queries/<CasoDeUso>/    Query + Handler + Dto
│   │   └── ApplicationServiceExtensions.cs   → AddApplication()
│   │
│   ├── Pedidos.Infrastructure/
│   │   ├── Persistence/     AppDbContext, ConexionSql,
│   │   │                    MapeoEntidades/ (Fluent API), Repositories/ (escritura)
│   │   ├── Queries/         implementaciones de IXConsultas (lectura)
│   │   ├── Caching/         RedisCacheService, DemoCache
│   │   ├── Mongo/           MongoAuditoriaColeccion, AuditoriaMongoRepository,
│   │   │                    AuditoriaMongoConsultas, AuditoriaDocumento
│   │   ├── Cosmos/          CosmosOpciones, CosmosInicializador, CatalogoDbContext,
│   │   │                    CatalogoRepository, Valoracion* (repositorio/consultas/documento)
│   │   ├── Storage/         BlobImagenesStorage
│   │   ├── Demo/            DemoMalasPracticas (solo Clase 4)
│   │   └── InfrastructureServiceExtensions.cs   → AddInfrastructure()
│   │
│   └── Pedidos.Api/
│       ├── Controllers/     Pedidos, Productos, Valoraciones, Auditoria, Cache
│       ├── Endpoints/       DemoClase4Endpoints, DemoClase5Endpoints (solo Development)
│       ├── Middleware/      AuditoriaMiddleware
│       ├── Infrastructure/  GlobalExceptionHandler
│       └── Program.cs
│
├── tests/
│   ├── Pedidos.Domain.Tests/           reglas del dominio y value objects
│   ├── Pedidos.Application.Tests/      handlers, validadores, CqrsArquitecturaTests (con fakes)
│   └── Pedidos.Infrastructure.Tests/   reglas del PedidoRepository
├── scripts/      SQL: crear BD, auditoría, procedimientos y seed de Clase 4
├── ejemplos/     JSON de prueba (válidos, 400, 409) + fotos de productos
├── docs/         ALMACENAMIENTO.md, clase4/clase5, tecnologias/ (redis, mongodb, cosmos-db, sql-server, blob)
└── docker-compose.yml
```

## 4. Inyección de dependencias (lo más importante de documentar)

La composición ocurre en **dos métodos de extensión**, llamados desde `Program.cs`:

```csharp
builder.Services.AddApplication().AddInfrastructure(builder.Configuration);
```

### 4.1 `AddApplication()` — `ApplicationServiceExtensions.cs`
- `AddMediatR` registrando todos los handlers del assembly.
- **Pipeline behaviors** (el orden importa: el primero envuelve al siguiente): `LoggingBehavior<,>` → `ValidationBehavior<,>`.
- `AddValidatorsFromAssembly` (FluentValidation).

### 4.2 `AddInfrastructure(configuration)` — `InfrastructureServiceExtensions.cs`

| Interfaz (Application) | Implementación (Infrastructure) | Ciclo de vida | Tecnología |
|---|---|---|---|
| `IAppDbContext` | `AppDbContext` | Scoped | EF Core / SQL Server |
| `IPedidoRepository`, `IProductoRepository` | `PedidoRepository`, `ProductoRepository` | Scoped | EF Core (escritura, con tracking) |
| `IPedidosConsultas`, `IProductoConsultas` | `PedidosConsultas`, `ProductosConsultas` | Scoped | EF Core (lectura, `AsNoTracking`, proyecta a DTO) |
| `IPedidosReportes` | `PedidosReportes` | Scoped | Dapper (procedimiento + SELECT/LINQ) |
| `IAuditoriaRepository` | `AuditoriaMongoRepository` | Singleton | **MongoDB** |
| `IAuditoriaConsultas` | `AuditoriaMongoConsultas` | Singleton | **MongoDB** |
| `ICacheService` | `RedisCacheService` | Singleton | **Redis** (`AddStackExchangeRedisCache`) |
| `IValoracionRepository`, `IValoracionConsultas` | `ValoracionCosmosRepository`, `ValoracionCosmosConsultas` | Singleton | Cosmos DB (SDK) |
| `ICatalogoRepository` | `CatalogoRepository` | Singleton | Cosmos DB (SDK) |
| (sin interfaz) `CatalogoDbContext` | — | Scoped | Cosmos DB (EF Core) |
| `IImagenesStorage` | `BlobImagenesStorage` | Singleton | Azure Blob / Azurite |
| `IDemoCache`, `IDemoCatalogo`, `IDemoCatalogoEf`, `IDemoMalasPracticas` | `DemoCache`, `DemoCatalogo`, `DemoCatalogoEf`, `DemoMalasPracticas` | Scoped/Singleton | Solo demos de clase |
| `TimeProvider` | `TimeProvider.System` | Singleton | — |

Detalles a mencionar:
- Clientes pesados (`MongoAuditoriaColeccion`, `CosmosClient`, `BlobServiceClient`) son **Singleton**: un solo cliente con su pool para toda la app.
- `ConexionSql` (Singleton) entrega la cadena a Dapper.
- Redis usa `abortConnect=false` y timeouts de 1 s: si Redis cae, la app **no falla al arrancar ni se queda esperando**.
- `AuditoriaMiddleware` se registra como **Transient** (instancia nueva por request).
- Cadenas de conexión (`ConnectionStrings`): `Pedidos` (obligatoria), `Mongo`, `Cosmos`, `Storage`, `Redis` (todas con valor por defecto local excepto `Pedidos`).

### 4.3 Pipeline HTTP en `Program.cs` (orden)
1. `AuditoriaMiddleware` (primero, para envolver al manejador de errores y registrar el código final).
2. `UseExceptionHandler` → `GlobalExceptionHandler`.
3. Solo Development: OpenAPI + Swagger UI, `MapDemoClase4()`, `MapDemoClase5()`, `InicializarCosmosAsync()`.
4. `UseHttpsRedirection`, `MapControllers`.

## 5. Flujo de un request (para el diagrama de secuencia)

`Cliente → Controller (sin lógica) → MediatR [Logging → Validation] → Handler → Dominio (reglas) → Repositorio (interfaz) → Infrastructure → respuesta`

Errores (todo como `ProblemDetails` vía `GlobalExceptionHandler`):

| Excepción | Origen | HTTP |
|---|---|---|
| `ValidationException` | FluentValidation | 400 |
| `NotFoundException` | Handlers | 404 |
| `ReglaNegocioException` | Dominio | 409 |
| Otra | — | 500 (sin detalles) |

## 6. Decisiones de diseño (para la diapositiva "por qué")

- **CQRS estricto:** comandos usan `IXRepository` (reglas + persistencia, con tracking); consultas usan `IXConsultas` (proyección directa a DTO, sin tracking). Lo verifican los tests de `CqrsArquitecturaTests`.
- **Validación en dos niveles:** FluentValidation valida la *forma*; el dominio protege las *reglas de negocio*.
- **Behaviors = patrón Decorator** sobre todos los handlers.
- **Controlador sin lógica**, DTOs sin lógica, sin mapper (proyección en la consulta).
- **Persistencia políglota** según necesidad: SQL Server (transaccional), Redis (caché), MongoDB (auditoría, esquema flexible y escritura rápida), Cosmos (catálogo/valoraciones), Blob (imágenes).
- **Application solo conoce interfaces:** cambiar Redis/Mongo no toca casos de uso.
- **MediatR 12.5.0** por licencia Apache 2.0 (la 13 requiere clave).

## 7. Entorno local (`docker-compose.yml`)

| Servicio | Puerto | Uso |
|---|---|---|
| redis | 6379 | Caché |
| redisinsight | 5540 | UI de Redis |
| mongo | 27017 | Auditoría (base `PedidosDb`, colección `auditoria`) |
| mongo-express | 8081 | UI de MongoDB |
| azurite | 10000-10002 | Blob/Queue/Table (imágenes) |
| cosmos (emulador vNext) | 8083 (HTTPS), 1234 | Catálogo y valoraciones; Data Explorer en :1234 |

SQL Server se usa vía LocalDB (no está en Docker). API: `https://localhost:7101/swagger`.

## 8. Qué debe generar el proyecto de documentación

1. **Diagrama de capas** (la regla de dependencias hacia el centro).
2. **Árbol de carpetas** (sección 3), resumido por capa.
3. **Tabla de inyección de dependencias** (sección 4.2) y el diagrama interfaz → implementación.
4. **Diagrama de secuencia** de `POST /api/pedidos` (sección 5).
5. **Diagrama de las bases de datos** y qué guarda cada una (SQL, Redis, Mongo, Cosmos, Blob).
6. **Manejo de errores** y **decisiones de diseño**.
7. **Evidencia** (capturas de Swagger, Redis Insight, Mongo Express, tests en verde).

## 9. Notas para quien documente

- El `README.md` aún describe la auditoría como tabla SQL `Auditoria` (`scripts/auditoria.sql`) y el árbol de Infrastructure sin Mongo/Redis/Cosmos/Blob. **La auditoría real está en MongoDB** (`AuditoriaMongoRepository`); la tabla/mapeo SQL de auditoría (`AuditoriaRepository`, `RegistroAuditoriaMapeo`) existe en el código pero no es lo que se registra en DI.
- Las demos de Clase 4 y Clase 5 (`/demo/...`) solo existen en Development: no son parte de la API de producción.
- Verificar con el código antes de afirmar números (endpoints, tests, cobertura).
