# INSTRUCCIONES · Clase 5 (Redis, Azure Storage y Cosmos DB)

Este archivo describe los cambios para agregar al proyecto las demos de la Clase 5 de la Certificación 2.
Está escrito para que lo siga una persona o un asistente de IA (Claude Code, Copilot): cada tarea dice qué archivos crear o modificar, con el código, y cómo verificar que quedó bien.

Ubicación sugerida en el repo: `docs/clase5/INSTRUCCIONES.md`.
Parte del proyecto como quedó en la Clase 4: `Ventas.Api`, `Ventas.Application`, `Ventas.Domain` y `Ventas.Infrastructure`, con Pedidos y Productos en SQL Server.

**Qué agrega esta clase:**
- **Redis:** caché delante de la lectura de productos (cache-aside).
- **Blob Storage (Azurite):** las imágenes de los productos.
- **Cosmos DB (emulador):** el catálogo con atributos distintos por categoría, primero con el SDK y después con EF Core.

Pedidos sigue en SQL Server. Las referencias a diapositivas (D10, D12…) son las del archivo `clase5-detalle.md`.

---

## Reglas generales

1. **Respetar la estructura existente.** Si en el proyecto los nombres son distintos, adaptar el código a los nombres reales (namespaces, carpetas, clases).
2. **Las reglas de negocio viven en Application**, en los handlers y los behaviors. Las entidades de Domain solo tienen datos.
3. **Domain no referencia a Redis, Azure ni Cosmos.** Todo eso va en Infrastructure. Application define interfaces cuando hace falta.
4. **No modificar los handlers, los behaviors ni lo hecho en la Clase 4**, salvo que una tarea lo indique.
5. **Una rama por tarea**, con la convención del curso `usuario/Tipo/issue` (por ejemplo `fprado/Feature/clase5-5.2-redis`), creada a partir de la rama de la tarea anterior.
6. **Compilar antes de seguir:** `dotnet build` sin errores al terminar cada tarea.
7. **Los endpoints de `/demo/clase5` son solo para la clase:** se mapean únicamente en Development.
8. **El endpoint `/catalogo/malo` arma la consulta concatenando texto a propósito** (demo de inyección). Queda solo en la rama de la tarea 5.5 y nunca se mergea a `main`.
9. **Clientes Singleton:** `CosmosClient`, `BlobServiceClient` y la conexión a Redis se registran una sola vez para toda la aplicación (diapositiva de lifetimes de la Clase 4).

## Orden de las tareas

| Tarea | Rama | Qué agrega |
|---|---|---|
| 0 | `…/clase5-0-preparacion` | Docker (Redis, Azurite y emulador de Cosmos), paquetes NuGet y cadenas de conexión |
| 5.1 | `…/clase5-5.1-decisiones` | Documento con la elección de almacenamiento de cada servicio |
| 5.2 | `…/clase5-5.2-redis` | Caché distribuida con cache-aside, expiración e invalidación |
| 5.3 | `…/clase5-5.3-blob` | Subida de imágenes de productos a Blob Storage |
| 5.4 | `…/clase5-5.4-cosmos` | Entidad `ProductoCatalogo`, cliente Cosmos, base, contenedor y datos de prueba |
| 5.5 | `…/clase5-5.5-sdk` | CRUD y consultas con el SDK, mostrando el costo en RU |
| 5.6 | `…/clase5-5.6-efcore-cosmos` | El mismo catálogo con el proveedor de EF Core para Cosmos |

---

## Tarea 0 · Preparación

**Rama:** `usuario/Feature/clase5-0-preparacion`

### 0.1 Servicios locales con Docker
Crear `docker-compose.clase5.yml` en la raíz del repo:
```yaml
services:
  redis:
    image: redis:7
    container_name: redis
    ports:
      - "6379:6379"

  azurite:
    image: mcr.microsoft.com/azure-storage/azurite
    container_name: azurite
    ports:
      - "10000:10000"   # Blob
      - "10001:10001"   # Queue
      - "10002:10002"   # Table

  cosmos:
    image: mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:vnext-preview
    container_name: cosmos
    ports:
      - "8081:8081"     # API
      - "1234:1234"     # Data Explorer (http://localhost:1234)
```
Levantar todo con `docker compose -f docker-compose.clase5.yml up -d`.

> El emulador `vnext-preview` de Cosmos es liviano y corre en Linux, Mac (ARM) y Windows, pero está en preview y no tiene todas las funciones de Cosmos. Antes de la clase, verificar que la imagen siga vigente en la documentación de Microsoft. Si algo no funciona, usar una cuenta de Azure (free tier) y cambiar solo la cadena de conexión.

### 0.2 Paquetes NuGet
En `Ventas.Infrastructure`:
```bash
dotnet add package Microsoft.Extensions.Caching.StackExchangeRedis
dotnet add package Azure.Storage.Blobs
dotnet add package Microsoft.Azure.Cosmos
dotnet add package Newtonsoft.Json              # el SDK de Cosmos lo pide como referencia explícita
dotnet add package Microsoft.EntityFrameworkCore.Cosmos   # misma versión que el resto de EF Core
```

### 0.3 Cadenas de conexión
En `appsettings.Development.json` de `Ventas.Api`:
```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379",
    "Storage": "UseDevelopmentStorage=true",
    "Cosmos": "AccountEndpoint=http://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="
  }
}
```
> La clave del emulador de Cosmos es pública y fija (es la misma para todos). Con una cuenta real de Azure, la clave va en user secrets, nunca en el repo.

### 0.4 Archivo de endpoints de demo
Crear `Ventas.Api/Endpoints/DemoClase5Endpoints.cs`:
```csharp
public static class DemoClase5Endpoints
{
    public static void MapDemoClase5(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/demo/clase5");
        // cada tarea agrega sus endpoints acá
    }
}
```
En `Program.cs`, junto a la Clase 4:
```csharp
if (app.Environment.IsDevelopment()) app.MapDemoClase5();
```

**Criterio de aceptación:** `docker ps` muestra `redis`, `azurite` y `cosmos` corriendo; el Data Explorer abre en `http://localhost:1234`; la solución compila.

---

## Tarea 5.1 · Elección de almacenamiento

**Rama:** `usuario/Feature/clase5-5.1-decisiones`

No lleva código. Crear `docs/ALMACENAMIENTO.md` con la decisión de cada servicio (es lo que pide el reto de la clase):

```markdown
# Almacenamiento por servicio

| Servicio | Dónde | Por qué (una línea) |
|---|---|---|
| Pedidos | SQL Server | Transacciones: el pedido, sus líneas y el pago se guardan juntos |
| Catálogo | Cosmos DB, partition key /categoria | Atributos distintos por categoría; casi siempre se lee filtrando por categoría |
| Lecturas de productos | Redis delante (cache-aside, 10 min) | Se leen mucho y cambian poco |
| Imágenes de productos | Blob Storage | Son archivos: la base guarda solo la URL |
```

**Para mostrar en clase:** abrir el archivo con las diapositivas D7 y D8 (criterios y "cada microservicio, su base").

**Criterio de aceptación:** existe `docs/ALMACENAMIENTO.md` con una fila por servicio y su justificación.

---

## Tarea 5.2 · Redis: caché distribuida y cache-aside

**Rama:** `usuario/Feature/clase5-5.2-redis`

### Registro (Ventas.Infrastructure/DependencyInjection.cs)
Agregar en `AddInfrastructure`:
```csharp
services.AddStackExchangeRedisCache(o =>
{
    o.Configuration = config.GetConnectionString("Redis");
    o.InstanceName = "ventas:";          // prefijo de todas las claves
});
```

### Endpoints de demo (agregar a `DemoClase5Endpoints`)
Usan la tabla `Productos` de SQL Server de la Clase 4 y el `ProductoDto(Guid Id, string Nombre, decimal Precio)` de `DemoClase4Endpoints`.
```csharp
// 5.2 · LO MALO: sin expiración
g.MapGet("/cache/malo/{id:guid}", async (Guid id, IDistributedCache cache, VentasDbContext db, CancellationToken ct) =>
{
    var clave = $"malo:producto:{id}";
    var json = await cache.GetStringAsync(clave, ct);
    if (json is not null)
        return Results.Ok(new { origen = "cache", producto = JsonSerializer.Deserialize<ProductoDto>(json) });

    var producto = await db.Productos.AsNoTracking()
        .Where(p => p.Id == id)
        .Select(p => new ProductoDto(p.Id, p.Nombre, p.Precio))
        .FirstOrDefaultAsync(ct);
    if (producto is null) return Results.NotFound();

    await cache.SetStringAsync(clave, JsonSerializer.Serialize(producto), ct);   // queda para siempre
    return Results.Ok(new { origen = "base", producto });
});

// 5.2 · LO MALO: actualiza el precio y no invalida la caché
g.MapPut("/cache/malo/{id:guid}/precio/{precio:decimal}", async (Guid id, decimal precio, VentasDbContext db, CancellationToken ct) =>
{
    await db.Productos.Where(p => p.Id == id)
        .ExecuteUpdateAsync(s => s.SetProperty(p => p.Precio, precio), ct);
    return Results.NoContent();                                                   // la caché sigue con el precio viejo
});

// 5.2 · LO BUENO: cache-aside con expiración
g.MapGet("/cache/bueno/{id:guid}", async (Guid id, IDistributedCache cache, VentasDbContext db, CancellationToken ct) =>
{
    var sw = Stopwatch.StartNew();
    var clave = $"producto:{id}";

    var json = await cache.GetStringAsync(clave, ct);                             // 1. buscar en caché
    if (json is not null)
        return Results.Ok(new { origen = "cache", ms = sw.ElapsedMilliseconds, producto = JsonSerializer.Deserialize<ProductoDto>(json) });

    var producto = await db.Productos.AsNoTracking()                              // 2. si no está, ir a la base
        .Where(p => p.Id == id)
        .Select(p => new ProductoDto(p.Id, p.Nombre, p.Precio))
        .FirstOrDefaultAsync(ct);
    if (producto is null) return Results.NotFound();

    await cache.SetStringAsync(clave, JsonSerializer.Serialize(producto),          // 3. guardar con expiración
        new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) }, ct);

    return Results.Ok(new { origen = "base", ms = sw.ElapsedMilliseconds, producto });
});

// 5.2 · LO BUENO: al actualizar, invalidar
g.MapPut("/cache/bueno/{id:guid}/precio/{precio:decimal}", async (Guid id, decimal precio, VentasDbContext db, IDistributedCache cache, CancellationToken ct) =>
{
    await db.Productos.Where(p => p.Id == id)
        .ExecuteUpdateAsync(s => s.SetProperty(p => p.Precio, precio), ct);
    await cache.RemoveAsync($"producto:{id}", ct);                                // la próxima lectura va a la base
    return Results.NoContent();
});
```
`Stopwatch` está en `System.Diagnostics`, `JsonSerializer` en `System.Text.Json` e `IDistributedCache` en `Microsoft.Extensions.Caching.Distributed`.

**Para mostrar en clase (D10 a D12):**
- Tomar el `Id` de un producto de la base y llamar dos veces a `/cache/bueno/{id}`: la primera dice `"origen": "base"` y la segunda `"origen": "cache"`, más rápida.
- Ver la clave en Redis: `docker exec -it redis redis-cli KEYS "ventas:*"` y `docker exec -it redis redis-cli TTL "ventas:producto:{id}"` (los segundos que le quedan).
- **Lo malo:** llamar a `/cache/malo/{id}`, cambiar el precio con `PUT /cache/malo/{id}/precio/999` y volver a leer: sigue el precio viejo, y `TTL` devuelve `-1` (no expira nunca).
- **Lo bueno:** lo mismo con `/cache/bueno`: después del `PUT`, la lectura dice `"origen": "base"` con el precio nuevo.

**Criterio de aceptación:** la segunda lectura de `/cache/bueno/{id}` sale de la caché; la clave tiene `TTL` positivo; después del `PUT` bueno, la lectura trae el precio nuevo; con el malo, trae el viejo.

---

## Tarea 5.3 · Blob Storage: imágenes de productos

**Rama:** `usuario/Feature/clase5-5.3-blob`

### Interfaz (Ventas.Application/Interfaces)
```csharp
public interface IImagenesStorage
{
    Task<string> SubirAsync(string nombre, Stream contenido, string contentType, CancellationToken ct);
}
```

### Implementación (Ventas.Infrastructure/Storage)
```csharp
public class BlobImagenesStorage(BlobServiceClient blobs) : IImagenesStorage
{
    public async Task<string> SubirAsync(string nombre, Stream contenido, string contentType, CancellationToken ct)
    {
        var contenedor = blobs.GetBlobContainerClient("productos");
        await contenedor.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: ct);

        var blob = contenedor.GetBlobClient(nombre);
        await blob.UploadAsync(contenido,
            new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } }, ct);

        return blob.Uri.ToString();                    // la base guarda solo esta URL
    }
}
```

### Registro (DependencyInjection.cs)
```csharp
services.AddSingleton(new BlobServiceClient(config.GetConnectionString("Storage")));
services.AddSingleton<IImagenesStorage, BlobImagenesStorage>();
```

### Endpoint de demo
```csharp
// 5.3 · Subir la imagen de un producto
g.MapPost("/imagenes/{productoId:guid}", async (Guid productoId, IFormFile archivo, IImagenesStorage storage, CancellationToken ct) =>
{
    await using var stream = archivo.OpenReadStream();
    var url = await storage.SubirAsync($"{productoId}{Path.GetExtension(archivo.FileName)}", stream, archivo.ContentType, ct);
    return Results.Ok(new { url });
}).DisableAntiforgery();
```

**Para mostrar en clase (D13 y D14):**
- Subir una foto: `curl -F "archivo=@foto.jpg" http://localhost:5000/demo/clase5/imagenes/{productoId}` (ajustar el puerto).
- Abrir la URL que devuelve en el navegador (`http://127.0.0.1:10000/devstoreaccount1/productos/...`): la imagen la sirve Blob, no la API ni la base.
- **Lo malo** (imagen como `varbinary(max)` en la base) va solo en la diapositiva; no se implementa.

**Criterio de aceptación:** el `POST` devuelve una URL; la URL abre la imagen; `IImagenesStorage` está en Application y `BlobImagenesStorage` en Infrastructure.

---

## Tarea 5.4 · Cosmos DB: cliente, base, contenedor y datos

**Rama:** `usuario/Feature/clase5-5.4-cosmos`

### Entidad (Ventas.Domain/Entities), solo datos
```csharp
public class ProductoCatalogo
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Categoria { get; set; } = "";          // partition key
    public string Nombre { get; set; } = "";
    public decimal Precio { get; set; }
    public Dictionary<string, string> Atributos { get; set; } = [];   // distintos por categoría
}
```

### Cliente (DependencyInjection.cs), Singleton
```csharp
services.AddSingleton(_ => new CosmosClient(config.GetConnectionString("Cosmos"), new CosmosClientOptions
{
    ConnectionMode = ConnectionMode.Gateway,           // necesario con el emulador vnext
    SerializerOptions = new CosmosSerializationOptions
    {
        PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase   // Id -> "id", Categoria -> "categoria"
    }
}));
```
> El camelCase hace que `Id` se guarde como `id`, que es lo que Cosmos exige, sin poner atributos de Cosmos en la entidad de Domain.

### Crear la base y el contenedor (Ventas.Infrastructure/Cosmos/CosmosInicializador.cs)
```csharp
public static class CosmosInicializador
{
    public static async Task InicializarCosmosAsync(this IServiceProvider sp)
    {
        var client = sp.GetRequiredService<CosmosClient>();
        var db = await client.CreateDatabaseIfNotExistsAsync("Ventas");
        await db.Database.CreateContainerIfNotExistsAsync("Productos", "/categoria");   // partition key
    }
}
```
En `Program.cs`, solo en Development y después de `builder.Build()`:
```csharp
if (app.Environment.IsDevelopment()) await app.Services.InicializarCosmosAsync();
```

### Datos de prueba (endpoint de demo)
```csharp
// 5.4 · Cargar 300 productos con atributos distintos por categoría
g.MapPost("/catalogo/seed", async (CosmosClient cosmos, CancellationToken ct) =>
{
    var contenedor = cosmos.GetContainer("Ventas", "Productos");
    string[] categorias = ["Bebidas", "Electro", "Indumentaria"];

    for (var i = 1; i <= 300; i++)
    {
        var categoria = categorias[i % 3];
        var producto = new ProductoCatalogo
        {
            Categoria = categoria,
            Nombre = $"{categoria} {i}",
            Precio = 1000 + i,
            Atributos = categoria switch
            {
                "Bebidas" => new() { ["litros"] = "1.5" },
                "Electro" => new() { ["pulgadas"] = "55", ["voltaje"] = "220V" },
                _         => new() { ["talle"] = "M", ["color"] = "Negro" }
            }
        };
        await contenedor.CreateItemAsync(producto, new PartitionKey(producto.Categoria), cancellationToken: ct);
    }
    return Results.Ok(new { cargados = 300 });
});
```

**Para mostrar en clase (D16 a D18 y D24):**
- En el Data Explorer (`http://localhost:1234`): cuenta → base `Ventas` → contenedor `Productos` → ítems.
- Abrir un producto de cada categoría y comparar el objeto `atributos`: mismo contenedor, distinta forma, sin columnas vacías ni migraciones.
- Mostrar la partition key `/categoria` en la configuración del contenedor.

**Criterio de aceptación:** existen la base `Ventas` y el contenedor `Productos` con partition key `/categoria`; después del seed hay 300 documentos con `id` y `categoria` en minúscula y atributos distintos por categoría.

---

## Tarea 5.5 · Cliente Cosmos DB (SDK): CRUD, consultas y RU

**Rama:** `usuario/Feature/clase5-5.5-sdk`

### Repositorio del catálogo
`Ventas.Application/Interfaces/ICatalogoRepository.cs`:
```csharp
public interface ICatalogoRepository
{
    Task<ProductoCatalogo?> ObtenerAsync(string id, string categoria, CancellationToken ct);
    Task<IReadOnlyList<ProductoCatalogo>> PorCategoriaAsync(string categoria, CancellationToken ct);
    Task GuardarAsync(ProductoCatalogo producto, CancellationToken ct);
}
```
`Ventas.Infrastructure/Cosmos/CatalogoRepository.cs`:
```csharp
public class CatalogoRepository(CosmosClient cosmos) : ICatalogoRepository
{
    private Container Contenedor => cosmos.GetContainer("Ventas", "Productos");

    public async Task<ProductoCatalogo?> ObtenerAsync(string id, string categoria, CancellationToken ct)
    {
        try
        {
            var r = await Contenedor.ReadItemAsync<ProductoCatalogo>(id, new PartitionKey(categoria), cancellationToken: ct);
            return r.Resource;                                           // point read: lo más barato
        }
        catch (CosmosException e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<ProductoCatalogo>> PorCategoriaAsync(string categoria, CancellationToken ct)
    {
        var consulta = new QueryDefinition("SELECT * FROM c WHERE c.categoria = @categoria")
            .WithParameter("@categoria", categoria);                     // parámetros, como en la Clase 4

        var it = Contenedor.GetItemQueryIterator<ProductoCatalogo>(consulta,
            requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(categoria) });

        var items = new List<ProductoCatalogo>();
        while (it.HasMoreResults) items.AddRange(await it.ReadNextAsync(ct));
        return items;
    }

    public Task GuardarAsync(ProductoCatalogo producto, CancellationToken ct)
        => Contenedor.UpsertItemAsync(producto, new PartitionKey(producto.Categoria), cancellationToken: ct);
}
```
Registro: `services.AddSingleton<ICatalogoRepository, CatalogoRepository>();` (no guarda estado propio y usa el `CosmosClient` Singleton).

### Endpoints de demo (muestran el costo en RU)
Agregar un método auxiliar dentro de `DemoClase5Endpoints`:
```csharp
private static async Task<(List<ProductoCatalogo> Items, double Ru)> LeerTodo(
    FeedIterator<ProductoCatalogo> it, CancellationToken ct)
{
    var items = new List<ProductoCatalogo>();
    double ru = 0;
    while (it.HasMoreResults)
    {
        var pagina = await it.ReadNextAsync(ct);
        items.AddRange(pagina);
        ru += pagina.RequestCharge;
    }
    return (items, ru);
}
```
Y los endpoints:
```csharp
// 5.5 · Point read: id + partition key
g.MapGet("/catalogo/{categoria}/{id}", async (string categoria, string id, CosmosClient cosmos, CancellationToken ct) =>
{
    var r = await cosmos.GetContainer("Ventas", "Productos")
        .ReadItemAsync<ProductoCatalogo>(id, new PartitionKey(categoria), cancellationToken: ct);
    return Results.Ok(new { ru = r.RequestCharge, producto = r.Resource });
});

// 5.5 · LO MALO: consulta concatenada (solo en esta rama)
g.MapGet("/catalogo/malo", async (string categoria, CosmosClient cosmos, CancellationToken ct) =>
{
    var it = cosmos.GetContainer("Ventas", "Productos")
        .GetItemQueryIterator<ProductoCatalogo>($"SELECT * FROM c WHERE c.categoria = '{categoria}'");
    var (items, ru) = await LeerTodo(it, ct);
    return Results.Ok(new { cantidad = items.Count, ru });
});

// 5.5 · LO BUENO: parámetros y partition key
g.MapGet("/catalogo/bueno", async (string categoria, CosmosClient cosmos, CancellationToken ct) =>
{
    var consulta = new QueryDefinition("SELECT * FROM c WHERE c.categoria = @categoria")
        .WithParameter("@categoria", categoria);
    var it = cosmos.GetContainer("Ventas", "Productos").GetItemQueryIterator<ProductoCatalogo>(consulta,
        requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(categoria) });
    var (items, ru) = await LeerTodo(it, ct);
    return Results.Ok(new { cantidad = items.Count, ru });
});

// 5.5 · Consulta que cruza particiones (sin partition key)
g.MapGet("/catalogo/cruzada", async (string nombre, CosmosClient cosmos, CancellationToken ct) =>
{
    var consulta = new QueryDefinition("SELECT * FROM c WHERE c.nombre = @nombre")
        .WithParameter("@nombre", nombre);
    var it = cosmos.GetContainer("Ventas", "Productos").GetItemQueryIterator<ProductoCatalogo>(consulta);
    var (items, ru) = await LeerTodo(it, ct);
    return Results.Ok(new { cantidad = items.Count, ru });
});
```

**Para mostrar en clase (D20 a D23):**
- **RU:** comparar el `ru` de `/catalogo/Electro/{id}` (point read, unas pocas RU) con `/catalogo/cruzada?nombre=Electro 5` (recorre todas las particiones, cuesta más).
- **Inyección:** `/catalogo/malo?categoria=' OR '1'='1` devuelve los 300 productos de todas las categorías; `/catalogo/bueno` con el mismo valor devuelve 0. La consulta de Cosmos es de solo lectura (no hay `DROP` por esta vía), pero igual expone datos que no correspondían: la regla es la misma, siempre parámetros.
- **Singleton:** mostrar en `DependencyInjection.cs` que `CosmosClient` se registra una sola vez (lo malo, `new CosmosClient` por request, va solo en la diapositiva).

> El endpoint `/catalogo/malo` arma la consulta concatenando texto a propósito. Dejalo solo en la rama de la demo y nunca lo subas a `main`.

**Criterio de aceptación:** el point read cuesta menos RU que la consulta cruzada; `/catalogo/malo?categoria=' OR '1'='1` devuelve 300 y `/catalogo/bueno` con el mismo valor devuelve 0; `ICatalogoRepository` está en Application y `CatalogoRepository` en Infrastructure.

---

## Tarea 5.6 · Proveedor de EF Core para Cosmos DB

**Rama:** `usuario/Feature/clase5-5.6-efcore-cosmos`

### DbContext para Cosmos (Ventas.Infrastructure/Cosmos/CatalogoDbContext.cs)
```csharp
public class CatalogoDbContext(DbContextOptions<CatalogoDbContext> options) : DbContext(options)
{
    public DbSet<ProductoCatalogo> Productos => Set<ProductoCatalogo>();

    protected override void OnModelCreating(ModelBuilder mb)
        => mb.Entity<ProductoCatalogo>(b =>
        {
            b.ToContainer("ProductosEf");                 // contenedor propio, separado del de la 5.5
            b.HasPartitionKey(p => p.Categoria);
            b.HasKey(p => p.Id);
        });
}
```
> La configuración va dentro de este DbContext, y no en una clase `IEntityTypeConfiguration`: el `VentasDbContext` de la Clase 4 carga todas las configuraciones del ensamblado con `ApplyConfigurationsFromAssembly` y tomaría también esta, que es de Cosmos.
>
> Se usa un contenedor aparte (`ProductosEf`) porque EF Core guarda los documentos con su propio formato (nombres de propiedades y un campo de tipo, el discriminador). Así se pueden comparar los dos formatos en el Data Explorer.

### Registro (DependencyInjection.cs)
```csharp
services.AddDbContext<CatalogoDbContext>(o =>
    o.UseCosmos(config.GetConnectionString("Cosmos")!, "Ventas",
        c => c.ConnectionMode(ConnectionMode.Gateway)));
```
En `Program.cs`, en Development, crear el contenedor:
```csharp
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<CatalogoDbContext>().Database.EnsureCreatedAsync();
}
```

### Endpoints de demo
```csharp
// 5.6 · Guardar con EF Core: AddRange y SaveChanges, igual que en SQL
g.MapPost("/ef/seed", async (CatalogoDbContext db, CancellationToken ct) =>
{
    db.Productos.AddRange(Enumerable.Range(1, 30).Select(i => new ProductoCatalogo
    {
        Categoria = i % 2 == 0 ? "Bebidas" : "Electro",
        Nombre = $"EF {i}",
        Precio = 500 + i,
        Atributos = i % 2 == 0 ? new() { ["litros"] = "2" } : new() { ["pulgadas"] = "32" }
    }));
    await db.SaveChangesAsync(ct);
    return Results.Ok(new { cargados = 30 });
});

// 5.6 · Consultar con LINQ dentro de una partición
g.MapGet("/ef/{categoria}", async (string categoria, CatalogoDbContext db, CancellationToken ct) =>
    await db.Productos
        .WithPartitionKey(categoria)
        .OrderBy(p => p.Nombre)
        .ToListAsync(ct));
```

**Para mostrar en clase (D26 y D27):**
- Lo que se parece a la Clase 4: `DbContext`, LINQ, `AddRange` y `SaveChanges`.
- En el Data Explorer, comparar un documento de `Productos` (SDK) con uno de `ProductosEf` (EF Core): mismos datos y distinto formato (nombres de propiedades y campo de tipo).
- **Límites:**
  - no hay migraciones (se usa `EnsureCreated`);
  - no hay joins entre documentos;
  - todas las operaciones son asíncronas;
  - una consulta sin `WithPartitionKey` cruza particiones.

> Si la versión de EF Core del proyecto no mapea el `Dictionary<string, string>` de `Atributos`, agregar `b.Ignore(p => p.Atributos);` y mostrarlo como otro límite del proveedor frente al SDK.

**Criterio de aceptación:** existe el contenedor `ProductosEf`; `/ef/seed` guarda 30 documentos con un solo `SaveChanges`; `/ef/Bebidas` devuelve solo los de esa categoría.

---

## Cómo pedírselo a un asistente de IA

Con este archivo en `docs/clase5/INSTRUCCIONES.md`, alcanza con decirle:

> Seguí `docs/clase5/INSTRUCCIONES.md`. Hacé las tareas en orden, de a una, respetando las reglas generales. Al terminar cada tarea, corré `dotnet build`, verificá el criterio de aceptación y listame los archivos que creaste o modificaste antes de pasar a la siguiente.
