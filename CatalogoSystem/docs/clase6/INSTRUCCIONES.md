# INSTRUCCIONES · Clase 6 (Catálogo con Minimal APIs y gRPC)

Este archivo describe cómo crear el microservicio **Catálogo** dentro de PedidosSystem y cómo conectarlo con **Pedidos** por gRPC, para las demos de la Clase 6 de la Certificación 2.
Está escrito para que lo siga una persona o un asistente de IA (Claude Code, Copilot): cada tarea dice qué archivos crear o modificar, con el código, y cómo verificar que quedó bien.

Ubicación sugerida en el repo: `docs/clase6/INSTRUCCIONES.md`.

> **Estado en CatalogoSystem** (agregado al copiar este archivo al repo):
> - Hechas: Tarea 0, 6.1, 6.2, 6.3 (incluye el endpoint `buscar` y `PROMPT-ENDPOINTS.md`), el servidor gRPC de 6.4/6.5 (6.5.1 y 6.5.2) y el interceptor de 6.6.1.
> - Pendientes: el cliente gRPC y las demos en **PedidosSystem** (6.5.3 y 6.5.4), TLS en clase (6.6.2) y JWT (6.6.3, opcional).
> - Diferencia: CatalogoSystem es una solución propia (no un proyecto dentro de PedidosSystem) y usa los puertos **https 7301 / http 5301**, porque ClientesSystem ya ocupa el 7201. Donde abajo dice `7201`, leer `7301`.

**Qué queda al final:**
```
Front / Swagger ──REST──▶ Catalogo.Api (Minimal APIs)  ─┐
                                                        ├──▶ mismo caso de uso (Application) ──▶ Cosmos DB
Pedidos.Api ─────gRPC───▶ Catalogo.Api (servicio gRPC) ─┘
Pedidos.Api ─────REST───▶ Clientes.Api (ya existe)
```

**Puntos de la clase que cubre:**

| Tarea | Cápsula |
|---|---|
| 6.1 | Minimal APIs: grupos, binding, filtros |
| 6.2 | OpenAPI, Swagger y `TypedResults` |
| 6.3 | Endpoint generado con IA y su revisión |
| 6.4 y 6.5 | `.proto` compartido, servidor gRPC y cliente |
| 6.6 | Interceptor y JWT |

---

## Reglas generales

1. **Misma arquitectura que Pedidos:**
   - `Catalogo.Domain`: entidades, solo datos;
   - `Catalogo.Application`: casos de uso con MediatR, behaviors, validadores e interfaces;
   - `Catalogo.Infrastructure`: Cosmos;
   - `Catalogo.Api`: Minimal APIs y gRPC.

   Las dependencias apuntan hacia el centro.
2. **Las reglas de negocio viven en Application.** Los endpoints y el servicio gRPC **no tienen lógica**: reciben, mandan un command o una query con `ISender` y responden.
3. **Minimal APIs y gRPC son dos puertas al mismo caso de uso.** Los dos llaman a las mismas queries y commands.
4. **Paquetes centralizados:** la solución usa `Directory.Packages.props`. Cada paquete nuevo se agrega ahí con `<PackageVersion>` y en el `.csproj` con `<PackageReference>` sin versión. MediatR se mantiene en **12.5.0**, igual que Pedidos (licencia Apache 2.0).
5. **No romper lo de las clases 4 y 5.** Las demos de Pedidos siguen funcionando: en Pedidos no se borra nada del catálogo de Cosmos.
6. **Una rama por tarea**, con la convención `usuario/Feature/clase6-6.x-...`, creada desde la anterior.
7. **Compilar antes de seguir:** `dotnet build` sin errores al terminar cada tarea.
8. **Los endpoints de `/demo/clase6` son solo para la clase** y se mapean únicamente en Development.

> **Decisión para el curso:** en un sistema real, Catálogo sería el **único dueño** de los datos del catálogo y Pedidos dejaría de leer Cosmos. Para no romper las demos de la Clase 5, Catálogo lee el **mismo contenedor de Cosmos** (`Productos`) con su propia copia de la entidad y del repositorio. Decirlo en clase.

## Orden de las tareas

| Tarea | Rama | Qué agrega |
|---|---|---|
| 0 | `…/clase6-0-catalogo-base` | Proyectos de Catálogo, referencias, paquetes, puertos |
| 6.1 | `…/clase6-6.1-minimal-apis` | Casos de uso y endpoints con `MapGroup`, binding y filtro |
| 6.2 | `…/clase6-6.2-openapi` | OpenAPI, Swagger UI, `TypedResults` y la demo de `IResult` |
| 6.3 | `…/clase6-6.3-ia` | Plantilla de prompt, endpoint generado con IA y su revisión |
| 6.4 y 6.5 | `…/clase6-6.5-grpc` | `.proto` compartido, servidor en Catálogo y cliente en Pedidos |
| 6.6 | `…/clase6-6.6-seguridad` | Interceptor de logging y (opcional) JWT |

---

## Tarea 0 · Proyectos de Catálogo

**Rama:** `usuario/Feature/clase6-0-catalogo-base`

### 0.1 Crear los proyectos
Desde la raíz del repo:
```bash
dotnet new classlib -n Catalogo.Domain         -o src/Catalogo.Domain
dotnet new classlib -n Catalogo.Application    -o src/Catalogo.Application
dotnet new classlib -n Catalogo.Infrastructure -o src/Catalogo.Infrastructure
dotnet new web      -n Catalogo.Api            -o src/Catalogo.Api

dotnet sln PedidosSystem.slnx add src/Catalogo.Domain src/Catalogo.Application src/Catalogo.Infrastructure src/Catalogo.Api

dotnet add src/Catalogo.Application    reference src/Catalogo.Domain
dotnet add src/Catalogo.Infrastructure reference src/Catalogo.Application
dotnet add src/Catalogo.Api            reference src/Catalogo.Application src/Catalogo.Infrastructure
```

### 0.2 Paquetes (en `Directory.Packages.props` y en cada `.csproj`)

| Proyecto | Paquetes |
|---|---|
| Catalogo.Application | `MediatR` (12.5.0), `FluentValidation`, `FluentValidation.DependencyInjectionExtensions` |
| Catalogo.Infrastructure | `Microsoft.Azure.Cosmos`, `Newtonsoft.Json` |
| Catalogo.Api | `Microsoft.AspNetCore.OpenApi`, `Swashbuckle.AspNetCore.SwaggerUI`, `Grpc.AspNetCore` |

Usar las mismas versiones que ya tenga Pedidos para los paquetes que se repiten.

### 0.3 Puertos y configuración
En `src/Catalogo.Api/Properties/launchSettings.json`: **https 7201** y **http 5201** (Pedidos sigue en 7101).

En `src/Catalogo.Api/appsettings.Development.json`, la misma cadena de Cosmos que usa Pedidos:
```json
{
  "ConnectionStrings": {
    "Cosmos": "(copiar la cadena de Pedidos.Api/appsettings.Development.json)"
  }
}
```

### 0.4 Dominio (Catalogo.Domain/Productos/ProductoCatalogo.cs), solo datos
Copiar `ProductoCatalogo` de `Pedidos.Domain/Catalogo` con las mismas propiedades. Si no tiene stock, agregarle `public int Stock { get; set; }`, que hace falta para gRPC.

### 0.5 Infraestructura (Catalogo.Infrastructure)
- **Copiar de `Pedidos.Infrastructure/Cosmos`:** `CosmosOpciones`, el registro de `CosmosClient` (Singleton) y `CatalogoRepository`, adaptando los namespaces a `Catalogo.*`. El contenedor y la partition key son **los mismos** (`Productos`, `/categoria`).
- **Crear `InfrastructureServiceExtensions.AddInfrastructure(configuration)`** que registre el `CosmosClient` y `ICatalogoRepository`.

**Criterio de aceptación:**
- la solución compila;
- `Catalogo.Api` arranca en `https://localhost:7201`;
- `Catalogo.Domain` no referencia ningún paquete.

---

## Tarea 6.1 · Minimal APIs: grupos, binding y filtros

**Rama:** `usuario/Feature/clase6-6.1-minimal-apis`

### Application: casos de uso
```
Catalogo.Application/
  Interfaces/ICatalogoRepository.cs
  Behaviors/LoggingBehavior.cs, ValidationBehavior.cs      ← copiar de Pedidos.Application
  Productos/
    ProductoDto.cs
    Queries/ObtenerProducto/ObtenerProductoQuery.cs + Handler
    Queries/ListarProductos/ListarProductosQuery.cs + Handler
    Commands/CambiarPrecio/CambiarPrecioCommand.cs + Handler + Validator
  ApplicationServiceExtensions.cs                          → AddApplication()
```
```csharp
public record ProductoDto(string Id, string Categoria, string Nombre, decimal Precio, int Stock);

public record ObtenerProductoQuery(string Categoria, string Id) : IRequest<ProductoDto?>;
public record ListarProductosQuery(string Categoria, int Pagina) : IRequest<IReadOnlyList<ProductoDto>>;
public record CambiarPrecioCommand(string Categoria, string Id, decimal Precio) : IRequest<bool>;

public class CambiarPrecioValidator : AbstractValidator<CambiarPrecioCommand>
{
    public CambiarPrecioValidator() => RuleFor(c => c.Precio).GreaterThan(0);
}
```
- **Los handlers usan `ICatalogoRepository`.** `ListarProductos` consulta por categoría (con la partition key) y pagina de a 20.
- **`AddApplication()`** registra MediatR, los behaviors (Logging → Validation, en ese orden) y los validadores, igual que Pedidos.

### Api: endpoints con MapGroup (Catalogo.Api/Endpoints/ProductosEndpoints.cs)
```csharp
public static class ProductosEndpoints
{
    public record CambiarPrecioRequest(decimal Precio);

    public static IEndpointRouteBuilder MapProductos(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/productos")
                   .WithTags("Productos")
                   .AddEndpointFilter<TiempoFilter>();            // filtro para todo el grupo

        g.MapGet("/", ListarAsync);
        g.MapGet("/{categoria}/{id}", ObtenerAsync);
        g.MapPut("/{categoria}/{id}/precio", CambiarPrecioAsync);
        return app;
    }

    // binding: categoria y pagina de la query, sender de DI, ct del framework
    static async Task<Ok<IReadOnlyList<ProductoDto>>> ListarAsync(
        string categoria, ISender sender, CancellationToken ct, int pagina = 1)
        => TypedResults.Ok(await sender.Send(new ListarProductosQuery(categoria, pagina), ct));

    // binding: categoria e id de la ruta
    static async Task<Results<Ok<ProductoDto>, NotFound>> ObtenerAsync(
        string categoria, string id, ISender sender, CancellationToken ct)
        => await sender.Send(new ObtenerProductoQuery(categoria, id), ct) is { } p
            ? TypedResults.Ok(p)
            : TypedResults.NotFound();

    // binding: el cuerpo en JSON
    static async Task<Results<NoContent, NotFound>> CambiarPrecioAsync(
        string categoria, string id, CambiarPrecioRequest body, ISender sender, CancellationToken ct)
        => await sender.Send(new CambiarPrecioCommand(categoria, id, body.Precio), ct)
            ? TypedResults.NoContent()
            : TypedResults.NotFound();
}
```

### Filtro de endpoint (Catalogo.Api/Filtros/TiempoFilter.cs)
```csharp
public class TiempoFilter(ILogger<TiempoFilter> log) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        var sw = Stopwatch.StartNew();
        var resultado = await next(ctx);                       // el endpoint
        ctx.HttpContext.Response.Headers["X-Tiempo-ms"] = sw.ElapsedMilliseconds.ToString();
        log.LogInformation("{Ruta} {Ms} ms", ctx.HttpContext.Request.Path, sw.ElapsedMilliseconds);
        return resultado;
    }
}
```

### Program.cs
```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApplication().AddInfrastructure(builder.Configuration);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();   // copiar de Pedidos.Api

var app = builder.Build();
app.UseExceptionHandler();
app.MapProductos();
app.Run();
```

**Para mostrar en clase (6.1):**
- **Pedidos (controllers) frente a Catálogo (Minimal APIs):** el mismo estilo de endpoint en las dos formas.
- **Las tres fuentes de binding:** ruta (`categoria`, `id`), query (`pagina`) y cuerpo (`CambiarPrecioRequest`).
- **El filtro:** el header `X-Tiempo-ms` en cada respuesta, y el log.

**Criterio de aceptación:**
- `GET /api/productos?categoria=Electro` devuelve productos;
- `GET /api/productos/Electro/{id}` devuelve 200 o 404;
- `PUT .../precio` con precio 0 devuelve 400 (lo frena el validador en el behavior);
- todas las respuestas traen `X-Tiempo-ms`;
- los endpoints no tienen lógica.

---

## Tarea 6.2 · OpenAPI, Swagger y TypedResults

**Rama:** `usuario/Feature/clase6-6.2-openapi`

### Program.cs
```csharp
builder.Services.AddOpenApi();
// ...
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();                                                         // /openapi/v1.json
    app.UseSwaggerUI(o => o.SwaggerEndpoint("/openapi/v1.json", "Catálogo"));
    app.MapDemoClase6();
}
```

### Documentar los endpoints
Agregar a cada endpoint de `ProductosEndpoints`:
- `.WithName("ObtenerProducto")`;
- `.WithSummary("Obtiene un producto por categoría e id")`;
- `.WithDescription(...)` donde haga falta.

### Demo de lo malo (Catalogo.Api/Endpoints/DemoClase6Endpoints.cs)
```csharp
public static class DemoClase6Endpoints
{
    public static void MapDemoClase6(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/demo/clase6").WithTags("Demo Clase 6");

        // LO MALO: IResult sin tipo; Swagger no sabe qué devuelve
        g.MapGet("/producto-malo/{categoria}/{id}", async (string categoria, string id, ISender sender, CancellationToken ct) =>
        {
            var p = await sender.Send(new ObtenerProductoQuery(categoria, id), ct);
            return p is null ? Results.NotFound() : Results.Ok(p);
        });
    }
}
```

**Para mostrar en clase (6.2):** abrir `https://localhost:7201/swagger` y comparar:
- `/demo/clase6/producto-malo/...`: muestra un 200 sin esquema;
- `/api/productos/{categoria}/{id}`: muestra 200 con `ProductoDto` y 404, sin anotar nada;
- `/openapi/v1.json`: el contrato en crudo.

**Criterio de aceptación:**
- Swagger muestra el grupo "Productos" con resumen en cada endpoint;
- el endpoint tipado documenta 200 y 404;
- el endpoint malo muestra solo 200 sin esquema;
- todo solo en Development.

---

## Tarea 6.3 · Endpoint generado con IA y su revisión

**Rama:** `usuario/Feature/clase6-6.3-ia`

### 6.3.1 Plantilla de prompt (docs/clase6/PROMPT-ENDPOINTS.md)
```markdown
# Plantilla para pedir endpoints a una IA

Minimal API en .NET 10, en `Catalogo.Api/Endpoints/ProductosEndpoints.cs`, grupo `/api/productos`.
Endpoint: <verbo y ruta> que <qué hace>.
- Usá MediatR (`ISender`) con una query o command nuevo en `Catalogo.Application/Productos`.
- Devolvé `TypedResults` con el tipo de retorno declarado (`Results<...>`).
- Recibí `CancellationToken` y pasalo a todo lo asíncrono.
- Devolvé DTOs, nunca entidades. Nada de lógica en el endpoint.
- Consultas a Cosmos con `QueryDefinition` y parámetros; nunca texto concatenado.
- Seguí el estilo de los endpoints que ya existen en el archivo.

## Checklist de revisión
- [ ] Valida la entrada
- [ ] Sin SQL ni consultas concatenadas; sin secretos
- [ ] Devuelve DTOs
- [ ] Maneja 404 y 400
- [ ] async y CancellationToken
- [ ] Sigue el estilo del proyecto
- [ ] Tiene al menos una prueba manual en Swagger
```

### 6.3.2 Generar y revisar
1. Pedirle a la IA, con la plantilla: `GET /api/productos/buscar?nombre=...` (búsqueda por nombre dentro de una categoría).
2. **Antes de aceptar,** revisar con el checklist y anotar en `PROMPT-ENDPOINTS.md`, en una sección "Revisión", qué se corrigió.
3. **Guardar también la primera versión** (aunque esté mal), en un bloque de código dentro de la misma sección, para mostrarla en clase.

**Para mostrar en clase (6.3):** la primera versión, con los problemas marcados, y la versión corregida.

**Criterio de aceptación:**
- existe `docs/clase6/PROMPT-ENDPOINTS.md` con la plantilla, el checklist y la revisión;
- el endpoint final usa `QueryDefinition` con parámetros, devuelve `ProductoDto` y `TypedResults`.

---

## Tarea 6.4 y 6.5 · Contrato .proto, servidor y cliente

**Rama:** `usuario/Feature/clase6-6.5-grpc`

### 6.5.1 El contrato compartido (protos/catalogo.proto, en la raíz del repo)
```proto
syntax = "proto3";
option csharp_namespace = "Catalogo.Grpc";
package catalogo;

service CatalogoService {
  rpc ObtenerPrecioYStock (ProductoRequest) returns (PrecioYStockReply);
  rpc SeguirStock         (ProductoRequest) returns (stream StockReply);   // streaming del servidor (opcional)
}

message ProductoRequest   { string producto_id = 1; string categoria = 2; }
message PrecioYStockReply { string producto_id = 1; double precio = 2; int32 stock = 3; }
message StockReply        { int32 stock = 1; }
```
> El servicio se llama `CatalogoService` y no `Catalogo` para que el código generado no choque con el namespace `Catalogo.*` de los proyectos. Las clases generadas son `CatalogoService.CatalogoServiceBase` (servidor) y `CatalogoService.CatalogoServiceClient` (cliente).
>
> La `categoria` viaja en el pedido porque es la partition key de Cosmos: así el servidor hace un *point read* (Clase 5).

### 6.5.2 El servidor (Catálogo)
En `src/Catalogo.Api/Catalogo.Api.csproj`:
```xml
<ItemGroup>
  <Protobuf Include="..\..\protos\catalogo.proto" GrpcServices="Server" Link="Protos\catalogo.proto" />
</ItemGroup>
```
`Catalogo.Api/Grpc/CatalogoGrpcService.cs`, la **misma query** que usa la Minimal API:
```csharp
public class CatalogoGrpcService(ISender sender) : CatalogoService.CatalogoServiceBase
{
    public override async Task<PrecioYStockReply> ObtenerPrecioYStock(ProductoRequest request, ServerCallContext context)
    {
        var p = await sender.Send(new ObtenerProductoQuery(request.Categoria, request.ProductoId), context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Producto {request.ProductoId} inexistente"));

        return new PrecioYStockReply { ProductoId = p.Id, Precio = (double)p.Precio, Stock = p.Stock };
    }

    // Opcional: streaming del servidor
    public override async Task SeguirStock(ProductoRequest request, IServerStreamWriter<StockReply> stream, ServerCallContext context)
    {
        for (var i = 0; i < 5 && !context.CancellationToken.IsCancellationRequested; i++)
        {
            var p = await sender.Send(new ObtenerProductoQuery(request.Categoria, request.ProductoId), context.CancellationToken);
            await stream.WriteAsync(new StockReply { Stock = p?.Stock ?? 0 });
            await Task.Delay(2000, context.CancellationToken);
        }
    }
}
```
En `Program.cs`:
```csharp
builder.Services.AddGrpc();
// ...
app.MapGrpcService<CatalogoGrpcService>();
```
> REST y gRPC conviven en el mismo puerto HTTPS (7201): Kestrel negocia HTTP/1.1 o HTTP/2 según quién llama. Si el certificado de desarrollo no está confiado: `dotnet dev-certs https --trust`.

### 6.5.3 El cliente (Pedidos)
Paquetes en `Pedidos.Infrastructure`: `Grpc.Net.ClientFactory`, `Google.Protobuf` y `Grpc.Tools` (este último con `PrivateAssets="all"`).

En `src/Pedidos.Infrastructure/Pedidos.Infrastructure.csproj`:
```xml
<ItemGroup>
  <Protobuf Include="..\..\protos\catalogo.proto" GrpcServices="Client" Link="Protos\catalogo.proto" />
</ItemGroup>
```
La interfaz, en Application (`Pedidos.Application/Interfaces/Catalogo/ICatalogoPrecios.cs`):
```csharp
public record PrecioYStock(decimal Precio, int Stock);

public interface ICatalogoPrecios
{
    Task<PrecioYStock?> ObtenerAsync(string categoria, string productoId, CancellationToken ct);
}
```
La implementación, en Infrastructure (`Pedidos.Infrastructure/Servicios/CatalogoPreciosGrpc.cs`):
```csharp
public class CatalogoPreciosGrpc(CatalogoService.CatalogoServiceClient cliente) : ICatalogoPrecios
{
    public async Task<PrecioYStock?> ObtenerAsync(string categoria, string productoId, CancellationToken ct)
    {
        try
        {
            var r = await cliente.ObtenerPrecioYStockAsync(
                new ProductoRequest { ProductoId = productoId, Categoria = categoria }, cancellationToken: ct);
            return new((decimal)r.Precio, r.Stock);
        }
        catch (RpcException e) when (e.StatusCode == StatusCode.NotFound)
        {
            return null;
        }
    }
}
```
El registro, en `InfrastructureServiceExtensions`:
```csharp
services.AddGrpcClient<CatalogoService.CatalogoServiceClient>(o =>
    o.Address = new Uri(configuration["Servicios:Catalogo"] ?? "https://localhost:7201"));
services.AddScoped<ICatalogoPrecios, CatalogoPreciosGrpc>();

// Para la comparación REST de la demo
services.AddHttpClient("catalogo-rest", c =>
    c.BaseAddress = new Uri(configuration["Servicios:Catalogo"] ?? "https://localhost:7201"));
```

### 6.5.4 Demo en Pedidos: REST frente a gRPC (Pedidos.Api/Endpoints/DemoClase6Endpoints.cs)
```csharp
public static class DemoClase6Endpoints
{
    public static void MapDemoClase6(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/demo/clase6");

        // gRPC: a través de la interfaz de Application
        g.MapGet("/catalogo-grpc/{categoria}/{id}", async (string categoria, string id, ICatalogoPrecios catalogo, CancellationToken ct) =>
        {
            var sw = Stopwatch.StartNew();
            var r = await catalogo.ObtenerAsync(categoria, id, ct);
            return r is null ? Results.NotFound() : Results.Ok(new { via = "gRPC", ms = sw.ElapsedMilliseconds, r.Precio, r.Stock });
        });

        // REST: el mismo dato por HTTP + JSON
        g.MapGet("/catalogo-rest/{categoria}/{id}", async (string categoria, string id, IHttpClientFactory f, CancellationToken ct) =>
        {
            var sw = Stopwatch.StartNew();
            var r = await f.CreateClient("catalogo-rest").GetFromJsonAsync<PrecioYStock>($"/api/productos/{categoria}/{id}", ct);
            return Results.Ok(new { via = "REST", ms = sw.ElapsedMilliseconds, r?.Precio, r?.Stock });
        });

        // REST frente a gRPC: 200 llamadas seguidas de cada uno
        g.MapGet("/comparar/{categoria}/{id}", async (string categoria, string id, ICatalogoPrecios catalogo, IHttpClientFactory f, CancellationToken ct) =>
        {
            var http = f.CreateClient("catalogo-rest");
            var sw = Stopwatch.StartNew();
            for (var i = 0; i < 200; i++) await http.GetFromJsonAsync<PrecioYStock>($"/api/productos/{categoria}/{id}", ct);
            var rest = sw.ElapsedMilliseconds;
            sw.Restart();
            for (var i = 0; i < 200; i++) await catalogo.ObtenerAsync(categoria, id, ct);
            return Results.Ok(new { llamadas = 200, restMs = rest, grpcMs = sw.ElapsedMilliseconds });
        });
    }
}
```
En `Program.cs` de Pedidos, solo en Development, junto a las demos anteriores: `app.MapDemoClase6();`.

**Para mostrar en clase (6.4 y 6.5):**
- **El contrato:** `protos/catalogo.proto`, compartido por los dos proyectos.
- **Las dos puertas en Catálogo:** `ProductosEndpoints` y `CatalogoGrpcService` llaman a la **misma** `ObtenerProductoQuery`.
- **En Pedidos:**
  - `/demo/clase6/catalogo-grpc/...` y `/demo/clase6/catalogo-rest/...` devuelven el mismo dato;
  - `/demo/clase6/comparar/...` muestra los tiempos.
  - Ojo: el resultado depende de la máquina; mostrarlo como tendencia, no como benchmark.
- **Romper el contrato:**
  - renombrar `stock` a `cantidad` en el `.proto` y compilar: Pedidos **no compila**;
  - con REST, el mismo cambio en el DTO de Catálogo hace que `Stock` llegue en 0 sin error.
  - Volver atrás después.

**Criterio de aceptación:**
- el `.proto` está en `protos/` y lo incluyen los dos proyectos;
- `/demo/clase6/catalogo-grpc/Electro/{id}` devuelve precio y stock;
- con un id inexistente devuelve 404, porque el `NotFound` de gRPC se traduce;
- `ICatalogoPrecios` está en Application y `CatalogoPreciosGrpc` en Infrastructure.

---

## Tarea 6.6 · Seguridad: interceptor y JWT

**Rama:** `usuario/Feature/clase6-6.6-seguridad`

### 6.6.1 Interceptor de logging (obligatorio) · Catalogo.Api/Grpc/LoggingInterceptor.cs
```csharp
public class LoggingInterceptor(ILogger<LoggingInterceptor> log) : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            return await continuation(request, context);
        }
        finally
        {
            log.LogInformation("gRPC {Metodo} {Ms} ms", context.Method, sw.ElapsedMilliseconds);
        }
    }
}
```
Registro: `builder.Services.AddGrpc(o => o.Interceptors.Add<LoggingInterceptor>());`

### 6.6.2 TLS
Ya está cubierto: Pedidos llama a `https://localhost:7201`. Para mostrarlo en clase, cambiar a `http://` en la configuración y ver que la llamada falla. Después, volver atrás.

### 6.6.3 JWT (opcional, si alcanza el tiempo)
En Catálogo:
```bash
dotnet user-jwts create --project src/Catalogo.Api --audience catalogo-grpc
```
El comando muestra un token y agrega la configuración de `Authentication:Schemes:Bearer` en `appsettings.Development.json`.

En `Catalogo.Api/Program.cs`:
```csharp
builder.Services.AddAuthentication("Bearer").AddJwtBearer();
builder.Services.AddAuthorization();
// ...
app.UseAuthentication();
app.UseAuthorization();
```
Y `[Authorize]` sobre `CatalogoGrpcService`. Los endpoints REST quedan anónimos para la demo de Swagger.

En Pedidos, guardar el token en user-secrets (`Servicios:CatalogoToken`) y mandarlo en los metadatos:
```csharp
services.AddGrpcClient<CatalogoService.CatalogoServiceClient>(o =>
        o.Address = new Uri(configuration["Servicios:Catalogo"] ?? "https://localhost:7201"))
    .AddCallCredentials((context, metadata) =>
    {
        metadata.Add("Authorization", $"Bearer {configuration["Servicios:CatalogoToken"]}");
        return Task.CompletedTask;
    });
```

**Para mostrar en clase (6.6):**
- **El log del interceptor** con el método y los milisegundos de cada llamada.
- **Con JWT:** sin el token, Pedidos recibe `Unauthenticated`; con el token, funciona.

**Criterio de aceptación:**
- cada llamada gRPC deja una línea de log con el método y el tiempo;
- si se hizo el JWT, una llamada sin token devuelve `StatusCode.Unauthenticated` y con token responde normalmente.

---

## Cómo pedírselo a un asistente de IA

Con este archivo en `docs/clase6/INSTRUCCIONES.md`, alcanza con decirle:

> Leé `docs/INSTRUCCIONES-documentacion.md` (o el README) para entender cómo está armado PedidosSystem, y después seguí `docs/clase6/INSTRUCCIONES.md`. Hacé las tareas en orden, de a una, respetando las reglas generales. Al terminar cada tarea, corré `dotnet build`, verificá el criterio de aceptación y listame los archivos que creaste o modificaste antes de pasar a la siguiente.
