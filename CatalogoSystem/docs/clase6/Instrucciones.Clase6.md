# Instrucciones · Clase 6 (índice de puntos)

Vista rápida de **todos los puntos de la Clase 6** (Catálogo con Minimal APIs y gRPC), con su estado y dónde está el código.
El detalle paso a paso de cada tarea está en [INSTRUCCIONES.md](INSTRUCCIONES.md); la plantilla de prompt para IA, en [PROMPT-ENDPOINTS.md](PROMPT-ENDPOINTS.md).

> CatalogoSystem es una solución propia y usa los puertos **https 7301 / http 5301** (donde INSTRUCCIONES.md dice `7201`, leer `7301`).

## Resumen

| Punto | Tema | Estado |
|---|---|---|
| 0 | Proyectos de Catálogo | Hecho |
| 6.1 | Minimal APIs: grupos, binding y filtros | Hecho |
| 6.2 | OpenAPI, Swagger y `TypedResults` | Hecho |
| 6.3 | Endpoint generado con IA y su revisión | Hecho |
| 6.4 | Contrato `.proto` compartido | Hecho |
| 6.5 | Servidor gRPC (Catálogo) y cliente + demo (Pedidos) | Hecho |
| 6.6.1 | Interceptor de logging | Hecho |
| 6.6.2 | TLS | Cubierto (solo demostrar en clase) |
| 6.6.3 | JWT | Hecho (emitido por AuthenticationSystem) |

---

## Tarea 0 · Proyectos de Catálogo
- [x] 0.1 Proyectos `Catalogo.Domain`, `Catalogo.Application`, `Catalogo.Infrastructure`, `Catalogo.Api` con sus referencias.
- [x] 0.2 Paquetes centralizados en `Directory.Packages.props` (MediatR 12.5.0).
- [x] 0.3 Puertos y configuración (`launchSettings.json`, `appsettings.Development.json`).
- [x] 0.4 Dominio: `ProductoCatalogo` solo con datos, incluye `Stock`.
- [x] 0.5 Infraestructura: Cosmos (`Productos`, partition key `/categoria`) y `AddInfrastructure`.

## Tarea 6.1 · Minimal APIs: grupos, binding y filtros
- [x] Casos de uso en Application: `ObtenerProductoQuery`, `ListarProductosQuery`, `CambiarPrecioCommand` (+ validador).
- [x] Behaviors Logging → Validation y `AddApplication()`.
- [x] `MapGroup("/api/productos")` con binding de ruta, query y cuerpo: `src/Catalogo.Api/Endpoints/ProductosEndpoints.cs`.
- [x] Filtro de endpoint con el header `X-Tiempo-ms`: `src/Catalogo.Api/Filtros/TiempoFilter.cs`.
- [x] Manejo global de excepciones con `ProblemDetails`.
- Verificar: `GET /api/productos?categoria=Electro`, `GET /api/productos/Electro/{id}` (200/404), `PUT .../precio` con precio 0 devuelve 400.

## Tarea 6.2 · OpenAPI, Swagger y TypedResults
- [x] `AddOpenApi()`, `MapOpenApi()` y Swagger UI solo en Development: `src/Catalogo.Api/Program.cs`.
- [x] `WithName`, `WithSummary` y `WithDescription` en cada endpoint.
- [x] Demo de lo malo (`IResult` sin tipo): `src/Catalogo.Api/Endpoints/DemoClase6Endpoints.cs` → `/demo/clase6/producto-malo/{categoria}/{id}`.
- Mostrar en clase: Swagger comparando el endpoint malo (200 sin esquema) con el tipado (200 `ProductoDto` y 404), y `/openapi/v1.json`.

## Tarea 6.3 · Endpoint generado con IA y su revisión
- [x] Plantilla de prompt, checklist y revisión: [PROMPT-ENDPOINTS.md](PROMPT-ENDPOINTS.md).
- [x] Endpoint `GET /api/productos/buscar?nombre=...` con `QueryDefinition` parametrizada, `ProductoDto` y `TypedResults`.
- Mostrar en clase: la primera versión con sus problemas y la versión corregida.

## Tarea 6.4 · Contrato .proto
- [x] Contrato compartido: `protos/catalogo.proto` (`CatalogoService`, `ObtenerPrecioYStock`, `SeguirStock`).
- [x] Incluido como `Server` en `Catalogo.Api.csproj` y como `Client` en `Pedidos.Infrastructure.csproj`.
- Mostrar en clase: romper el contrato (renombrar `stock`) y ver que Pedidos no compila.

## Tarea 6.5 · Servidor y cliente gRPC
- [x] 6.5.1 Contrato `.proto`.
- [x] 6.5.2 Servidor en Catálogo: `src/Catalogo.Api/Grpc/CatalogoGrpcService.cs`, misma `ObtenerProductoQuery` que la Minimal API; `AddGrpc` + `MapGrpcService`.
- [x] 6.5.3 Cliente en Pedidos: `ICatalogoGrpcClient` / `CatalogoGrpcClient` (`PedidosSystem/src/Pedidos.Infrastructure/Grpc/Catalogo/CatalogoGrpcClient.cs`), `NotFound` traducido a `null`.
- [x] 6.5.4 Demo REST frente a gRPC en Pedidos: `PedidosSystem/src/Pedidos.Api/Endpoints/DemoClase6Endpoints.cs` y `PedidosSystem/DemoClase6.http`:
  - `/demo/clase6/catalogo-rest/{categoria}/{id}`
  - `/demo/clase6/catalogo-grpc/{categoria}/{id}`
  - `/demo/clase6/comparar/{categoria}/{id}`
- Diferencia con INSTRUCCIONES.md: nombres `ICatalogoGrpcClient` / `CatalogoGrpcClient` en lugar de `ICatalogoPrecios` / `CatalogoPreciosGrpc`.

## Tarea 6.6 · Seguridad
- [x] 6.6.1 Interceptor de logging: `src/Catalogo.Api/Grpc/LoggingInterceptor.cs` (solo llamadas unarias; `SeguirStock` no se loguea).
- [x] 6.6.2 TLS: Pedidos llama a `https://localhost:7301/`. Para la clase, cambiar a `http://` y ver que falla.
- [x] 6.6.3 JWT: emitido por AuthenticationSystem (`POST api/tokens`), validado en `Catalogo.Api/Program.cs`, política `PedidosService` sobre `CatalogoGrpcService`; Pedidos lo obtiene y cachea con `AuthenticationTokenProvider`.
- Mostrar en clase: el log del interceptor y que sin token Pedidos recibe `Unauthenticated`.

## Fuera de alcance
- mTLS (certificados de cliente).
- Rate limiting y permisos por método.
