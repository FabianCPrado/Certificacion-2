# INSTRUCCIONES · Clase 4 (EF Core, LINQ y Dapper)

Este archivo describe los cambios para agregar al proyecto las demos de la Clase 4 de la Certificación 2.
Está escrito para que lo siga una persona o un asistente de IA (Claude Code, Copilot): cada tarea dice qué archivos crear o modificar, con el código, y cómo verificar que quedó bien.

Ubicación sugerida en el repo: `docs/clase4/INSTRUCCIONES.md`.

---

## Reglas generales

1. **Respetar la estructura existente.** La solución usa arquitectura limpia: `Ventas.Api`, `Ventas.Application`, `Ventas.Domain` y `Ventas.Infrastructure`. Si en el proyecto los nombres son distintos, adaptar el código a los nombres reales (namespaces, carpetas, clases).
2. **Las reglas de negocio viven en Application**, en los handlers y los behaviors. Las entidades de Domain solo tienen datos. No mover reglas a las entidades.
3. **No modificar los handlers ni los behaviors existentes**, salvo que una tarea lo indique.
4. **Domain no referencia a EF Core.** Todo lo de base de datos va en Infrastructure.
5. **Una rama por tarea**, con la convención del curso `usuario/Tipo/issue` (por ejemplo `fprado/Feature/clase4-4.1-dbcontext`), creada a partir de la rama de la tarea anterior.
6. **Compilar antes de seguir:** `dotnet build` sin errores al terminar cada tarea.
7. **Los endpoints de `/demo/clase4` son solo para la clase:** se mapean únicamente en Development.
8. **El endpoint `/sql/malo` es vulnerable a propósito** (demo de SQL injection). Queda solo en la rama de la tarea 4.5 y nunca se mergea a `main`.

## Orden de las tareas

| Tarea | Rama | Qué agrega |
|---|---|---|
| 4.1 | `…/clase4-4.1-dbcontext` | Entidades, `VentasDbContext`, registro con inyección de dependencias |
| 4.2 | `…/clase4-4.2-fluent-api` | Configuraciones Fluent API, base creada y datos de prueba |
| 4.3 | `…/clase4-4.3-linq` | Endpoints de demo de LINQ |
| 4.4 | `…/clase4-4.4-consultas` | Repositorio, handler de confirmar, proyecciones y `ExecuteUpdate` |
| 4.5 | `…/clase4-4.5-rendimiento` | N+1, SQL directo y procedimiento almacenado |
| 4.6 | `…/clase4-4.6-dapper` | `IVentasConsultas` con Dapper y reporte |

---

## Tarea 4.1 · DbContext e inyección de dependencias

**Rama:** `usuario/Feature/clase4-4.1-dbcontext`

### Entidades (Ventas.Domain), solo datos
```csharp
public enum EstadoPedido { Borrador, Confirmado, Anulado }

public class Pedido
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClienteId { get; set; }
    public DateTime FechaAlta { get; set; } = DateTime.UtcNow;
    public EstadoPedido Estado { get; set; } = EstadoPedido.Borrador;
    public decimal Total { get; set; }
    public List<LineaPedido> Lineas { get; set; } = [];
}

public class LineaPedido
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PedidoId { get; set; }
    public Guid ProductoId { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
}

public class Producto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nombre { get; set; } = "";
    public string Categoria { get; set; } = "";
    public decimal Precio { get; set; }
    public int Stock { get; set; }
}

public class Cliente
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nombre { get; set; } = "";
    public string Email { get; set; } = "";
}
```

### DbContext (Ventas.Infrastructure/Persistence)
```csharp
public class VentasDbContext(DbContextOptions<VentasDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<LineaPedido> LineasPedido => Set<LineaPedido>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Cliente> Clientes => Set<Cliente>();

    protected override void OnModelCreating(ModelBuilder mb)
        => mb.ApplyConfigurationsFromAssembly(typeof(VentasDbContext).Assembly);

    // Unidad de trabajo (Clase 3)
    public Task<int> GuardarCambiosAsync(CancellationToken ct = default)
        => SaveChangesAsync(ct);
}
```
> `LineasPedido` como `DbSet` propio está solo para poder mostrar el problema N+1 en la 4.5. En el código "bueno", las líneas se acceden siempre a través del pedido.

### Registro (Ventas.Infrastructure/DependencyInjection.cs)
```csharp
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<VentasDbContext>(o =>
            o.UseSqlServer(config.GetConnectionString("Ventas")));   // Scoped

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<VentasDbContext>());
        // services.AddScoped<IPedidoRepository, PedidoRepository>();  // se activa en la 4.4
        // services.AddScoped<IVentasConsultas, VentasConsultas>();    // se activa en la 4.6
        return services;
    }
}
```
En `Program.cs`: `builder.Services.AddInfrastructure(builder.Configuration);`

Si tu `IUnitOfWork` de la Clase 3 tiene otra firma, adaptá `GuardarCambiosAsync` a la tuya.

**Para mostrar en clase:** la versión "mala" (`new VentasDbContext()` y `AddSingleton`) solo en la diapositiva. En el código, mostrar que el `DbContext` llega por constructor y que `AddDbContext` lo registra Scoped.

**Criterio de aceptación:** la solución compila; `VentasDbContext` se recibe por constructor y está registrado con `AddDbContext` (Scoped); `Ventas.Domain` no tiene referencias a EF Core.

---

## Tarea 4.2 · Mapeo con Fluent API

**Rama:** `usuario/Feature/clase4-4.2-fluent-api`

`Ventas.Infrastructure/Persistence/Configurations/`:
```csharp
public class PedidoConfiguration : IEntityTypeConfiguration<Pedido>
{
    public void Configure(EntityTypeBuilder<Pedido> b)
    {
        b.ToTable("Pedidos");
        b.HasKey(p => p.Id);
        b.Property(p => p.Estado).HasConversion<string>().HasMaxLength(20);
        b.Property(p => p.Total).HasPrecision(18, 2);
        b.HasMany(p => p.Lineas).WithOne().HasForeignKey(l => l.PedidoId);
        b.HasIndex(p => p.ClienteId);
    }
}

public class LineaPedidoConfiguration : IEntityTypeConfiguration<LineaPedido>
{
    public void Configure(EntityTypeBuilder<LineaPedido> b)
    {
        b.ToTable("LineasPedido");
        b.HasKey(l => l.Id);
        b.Property(l => l.PrecioUnitario).HasPrecision(18, 2);
    }
}

public class ProductoConfiguration : IEntityTypeConfiguration<Producto>
{
    public void Configure(EntityTypeBuilder<Producto> b)
    {
        b.ToTable("Productos");
        b.HasKey(p => p.Id);
        b.Property(p => p.Nombre).HasMaxLength(200);
        b.Property(p => p.Categoria).HasMaxLength(50);
        b.Property(p => p.Precio).HasPrecision(18, 2);
        b.HasIndex(p => p.Categoria);
    }
}

public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> b)
    {
        b.ToTable("Clientes");
        b.HasKey(c => c.Id);
        b.Property(c => c.Nombre).HasMaxLength(200);
        b.Property(c => c.Email).HasMaxLength(200);
    }
}
```

### Crear la base (paso técnico, no se muestra en las diapositivas)
```bash
dotnet ef migrations add Inicial --project Ventas.Infrastructure --startup-project Ventas.Api
dotnet ef database update     --project Ventas.Infrastructure --startup-project Ventas.Api
```

### Datos de prueba
Correr `seed-clase4.sql` (anexo al final de este archivo) en la base `Ventas`. Carga 50.000 productos, 100 clientes y 1.000 pedidos con 3 líneas cada uno, y crea el procedimiento `ActualizarStock`.

**Para mostrar en clase:** abrir la base (Azure Data Studio, SSMS o la extensión de VS Code) y mostrar las tablas `Pedidos` y `LineasPedido` con la clave foránea. Mostrar que `Ventas.Domain` no tiene ninguna referencia a EF Core.

**Criterio de aceptación:** existen las tablas `Pedidos`, `LineasPedido`, `Productos` y `Clientes` con la clave foránea `PedidoId`, y tienen los datos del script de la sección final.

---

## Tarea 4.3 · LINQ con EF Core

**Rama:** `usuario/Feature/clase4-4.3-linq`

Endpoints de demo en `Ventas.Api/Endpoints/DemoClase4Endpoints.cs`. Son solo para la clase: usan el `DbContext` directo para comparar lo malo y lo bueno.

```csharp
public record ProductoDto(Guid Id, string Nombre, decimal Precio);

public static class DemoClase4Endpoints
{
    public static void MapDemoClase4(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/demo/clase4");

        // 4.3 · LO MALO: ToList demasiado temprano
        g.MapGet("/linq/malo", (VentasDbContext db) =>
            db.Productos.ToList()                     // trae toda la tabla
              .Where(p => p.Precio > 1000)
              .OrderBy(p => p.Nombre)
              .Take(20)
              .Select(p => new ProductoDto(p.Id, p.Nombre, p.Precio)));

        // 4.3 · LO BUENO: ejecutar al final
        g.MapGet("/linq/bueno", async (VentasDbContext db, int pagina, CancellationToken ct) =>
            await db.Productos
                .Where(p => p.Precio > 1000)
                .OrderBy(p => p.Nombre)
                .Skip((pagina - 1) * 20).Take(20)
                .Select(p => new ProductoDto(p.Id, p.Nombre, p.Precio))
                .ToListAsync(ct));

        // 4.3 · Ver el SQL sin ejecutarlo
        g.MapGet("/linq/sql", (VentasDbContext db) =>
            db.Productos
              .Where(p => p.Precio > 1000)
              .OrderBy(p => p.Nombre)
              .Take(20)
              .ToQueryString());
    }
}
```
En `Program.cs` (solo en Development):
```csharp
if (app.Environment.IsDevelopment()) app.MapDemoClase4();
```

**Para mostrar en clase:** llamar a `/linq/malo` y a `/linq/bueno?pagina=1` y comparar en la consola el SQL de cada uno (sin `WHERE` frente a `WHERE ... ORDER BY ... OFFSET ... FETCH`) y el tiempo de respuesta. `/linq/sql` devuelve el SQL como texto.

**Criterio de aceptación:** `/demo/clase4/linq/malo` y `/demo/clase4/linq/bueno?pagina=1` responden, y en la consola se ve que solo el segundo genera `WHERE`, `ORDER BY` y `OFFSET … FETCH`.

---

## Tarea 4.4 · Consultas, actualizaciones y transacciones

**Rama:** `usuario/Feature/clase4-4.4-consultas`

### Repositorio (Ventas.Infrastructure/Repositories)
Descomentá en `DependencyInjection` la línea de `IPedidoRepository`.
```csharp
public class PedidoRepository(VentasDbContext db) : IPedidoRepository
{
    public Task<Pedido?> ObtenerAsync(Guid id, CancellationToken ct)
        => db.Pedidos.Include(p => p.Lineas).FirstOrDefaultAsync(p => p.Id == id, ct);

    public void Agregar(Pedido pedido) => db.Pedidos.Add(pedido);
}
```

### Handler de confirmar (Ventas.Application), reglas en Application
```csharp
public class ConfirmarPedidoHandler(IPedidoRepository repo, IUnitOfWork uow)
{
    public async Task HandleAsync(Guid pedidoId, CancellationToken ct)
    {
        var pedido = await repo.ObtenerAsync(pedidoId, ct)
            ?? throw new KeyNotFoundException($"Pedido {pedidoId}");

        if (pedido.Lineas.Count == 0)
            throw new InvalidOperationException("No se puede confirmar un pedido vacío");

        pedido.Estado = EstadoPedido.Confirmado;
        await uow.GuardarCambiosAsync(ct);          // una transacción
    }
}
```
Adaptalo a tu forma de handlers y behaviors (si la validación ya vive en un behavior, el `if` va ahí).

### Endpoints de demo (agregar a `DemoClase4Endpoints`)
```csharp
public record PedidoResumenDto(Guid Id, string Estado, decimal Total);

// 4.4 · LO MALO: devolver la entidad, con todo y con tracking
g.MapGet("/pedidos/malo/{clienteId:guid}", async (VentasDbContext db, Guid clienteId, CancellationToken ct) =>
    await db.Pedidos.Include(p => p.Lineas)
        .Where(p => p.ClienteId == clienteId)
        .ToListAsync(ct));

// 4.4 · LO BUENO: AsNoTracking y proyección a DTO
g.MapGet("/pedidos/bueno/{clienteId:guid}", async (VentasDbContext db, Guid clienteId, CancellationToken ct) =>
    await db.Pedidos.AsNoTracking()
        .Where(p => p.ClienteId == clienteId)
        .Select(p => new PedidoResumenDto(p.Id, p.Estado.ToString(), p.Total))
        .ToListAsync(ct));

// 4.4 · Actualización masiva sin traer los datos
g.MapPost("/productos/aumentar/{categoria}", async (VentasDbContext db, string categoria, CancellationToken ct) =>
{
    var filas = await db.Productos
        .Where(p => p.Categoria == categoria)
        .ExecuteUpdateAsync(s => s.SetProperty(p => p.Precio, p => p.Precio * 1.10m), ct);
    return Results.Ok(new { filas });
});
```

**Para mostrar en clase:**
- Comparar en la consola el SQL de `/pedidos/malo` (todas las columnas y el JOIN) y `/pedidos/bueno` (tres columnas).
- Confirmar un pedido con el handler y mostrar que `SaveChanges` genera un solo `UPDATE`.
- `/productos/aumentar/Bebidas`: mostrar el `UPDATE ... WHERE Categoria = ...` sin ningún `SELECT` previo.

**Criterio de aceptación:** el handler confirma un pedido con un solo `UPDATE`; `/pedidos/bueno/{clienteId}` genera un `SELECT` de tres columnas; `/productos/aumentar/Bebidas` genera un `UPDATE` sin `SELECT` previo.

---

## Tarea 4.5 · Rendimiento y SQL directo

**Rama:** `usuario/Feature/clase4-4.5-rendimiento`

```csharp
public record LineaDto(Guid ProductoId, int Cantidad);
public record PedidoConLineasDto(Guid Id, List<LineaDto> Lineas);

// 4.5 · LO MALO: N+1
g.MapGet("/n1/malo", async (VentasDbContext db) =>
{
    var pedidos = await db.Pedidos.Take(100).ToListAsync();          // 1 consulta
    var resultado = new List<PedidoConLineasDto>();
    foreach (var p in pedidos)
    {
        var lineas = await db.LineasPedido                           // + 1 por pedido
            .Where(l => l.PedidoId == p.Id).ToListAsync();
        resultado.Add(new(p.Id, lineas.Select(l => new LineaDto(l.ProductoId, l.Cantidad)).ToList()));
    }
    return resultado;
});

// 4.5 · LO BUENO: una consulta
g.MapGet("/n1/bueno", async (VentasDbContext db, CancellationToken ct) =>
    await db.Pedidos.AsNoTracking().Take(100)
        .Select(p => new PedidoConLineasDto(p.Id,
            p.Lineas.Select(l => new LineaDto(l.ProductoId, l.Cantidad)).ToList()))
        .ToListAsync(ct));

// 4.5 · LO MALO: SQL injection (solo para la demo, nunca en producción)
g.MapGet("/sql/malo", async (VentasDbContext db, string nombre) =>
    await db.Productos
        .FromSqlRaw("SELECT * FROM Productos WHERE Nombre = '" + nombre + "'")
        .ToListAsync());

// 4.5 · LO BUENO: parámetros
g.MapGet("/sql/bueno", async (VentasDbContext db, string nombre, CancellationToken ct) =>
    await db.Productos
        .FromSql($"SELECT * FROM Productos WHERE Nombre = {nombre}")
        .AsNoTracking().ToListAsync(ct));

// 4.5 · Procedimiento almacenado
g.MapPost("/stock/{productoId:guid}/{cantidad:int}", async (VentasDbContext db, Guid productoId, int cantidad, CancellationToken ct) =>
{
    await db.Database.ExecuteSqlAsync($"EXEC ActualizarStock {productoId}, {cantidad}", ct);
    return Results.NoContent();
});
```

**Para mostrar en clase:**
- `/n1/malo`: contar en la consola las 101 consultas. `/n1/bueno`: una sola.
- `/sql/malo?nombre=' OR '1'='1`: devuelve todos los productos. Mostrar que Sonar marca la línea (regla S2077, Clase 1).
- `/sql/bueno?nombre=' OR '1'='1`: no devuelve nada, porque el valor viaja como parámetro (`@p0` en la consola).

> El endpoint `/sql/malo` es vulnerable a propósito. Dejalo solo en la rama de la demo y nunca lo subas a `main`.

**Criterio de aceptación:** `/n1/malo` genera 101 consultas y `/n1/bueno` una sola; `/sql/malo?nombre=' OR '1'='1` devuelve todo y `/sql/bueno` con el mismo valor no devuelve nada; el procedimiento `ActualizarStock` descuenta stock.

---

## Tarea 4.6 · Dapper y la capa de datos

**Rama:** `usuario/Feature/clase4-4.6-dapper`

### Interfaz de lectura (Ventas.Application/Interfaces)
```csharp
public record VentaPorDiaDto(DateTime Dia, decimal Total);

public interface IVentasConsultas
{
    Task<IReadOnlyList<VentaPorDiaDto>> VentasPorDiaAsync(
        DateTime desde, DateTime hasta, CancellationToken ct);
}
```

### Implementación con Dapper (Ventas.Infrastructure/Queries)
Descomentá en `DependencyInjection` la línea de `IVentasConsultas`.
```csharp
public class VentasConsultas(IConfiguration config) : IVentasConsultas
{
    public async Task<IReadOnlyList<VentaPorDiaDto>> VentasPorDiaAsync(
        DateTime desde, DateTime hasta, CancellationToken ct)
    {
        const string sql = """
            SELECT CAST(FechaAlta AS date) AS Dia, SUM(Total) AS Total
            FROM Pedidos
            WHERE Estado = 'Confirmado' AND FechaAlta >= @desde AND FechaAlta < @hasta
            GROUP BY CAST(FechaAlta AS date)
            ORDER BY Dia
            """;

        await using var cn = new SqlConnection(config.GetConnectionString("Ventas"));
        var filas = await cn.QueryAsync<VentaPorDiaDto>(
            new CommandDefinition(sql, new { desde, hasta }, cancellationToken: ct));
        return filas.ToList();
    }
}
```

### Endpoints de demo
```csharp
// 4.6 · LO MALO: agrupar en memoria con EF Core
g.MapGet("/reporte/malo", async (VentasDbContext db) =>
{
    var pedidos = await db.Pedidos.ToListAsync();               // toda la tabla
    return pedidos
        .Where(p => p.Estado == EstadoPedido.Confirmado)
        .GroupBy(p => p.FechaAlta.Date)
        .Select(gr => new VentaPorDiaDto(gr.Key, gr.Sum(p => p.Total)))
        .OrderBy(v => v.Dia);
});

// 4.6 · LO BUENO: Dapper a través de la interfaz de Application
g.MapGet("/reporte/bueno", (IVentasConsultas consultas, DateTime desde, DateTime hasta, CancellationToken ct) =>
    consultas.VentasPorDiaAsync(desde, hasta, ct));
```

**Para mostrar en clase:**
- Comparar `/reporte/malo` (trae todos los pedidos) con `/reporte/bueno?desde=2026-01-01&hasta=2027-01-01` (la base agrupa).
- Recorrer el proyecto con el diagrama de la diapositiva 23: `IVentasConsultas` en Application, `VentasConsultas` (Dapper) y `PedidoRepository` (EF Core) en Infrastructure.

**Criterio de aceptación:** `/reporte/bueno?desde=2026-01-01&hasta=2027-01-01` devuelve ventas por día; `IVentasConsultas` está en Application y `VentasConsultas` (Dapper) en Infrastructure.

---

## Anexo · Script de datos de prueba (`seed-clase4.sql`)

```sql
SET NOCOUNT ON;

-- 50.000 productos
;WITH n AS (
    SELECT TOP (50000) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS i
    FROM sys.all_objects a CROSS JOIN sys.all_objects b
)
INSERT INTO Productos (Id, Nombre, Categoria, Precio, Stock)
SELECT NEWID(),
       CONCAT('Producto ', i),
       CASE i % 4 WHEN 0 THEN 'Bebidas' WHEN 1 THEN 'Almacén' WHEN 2 THEN 'Limpieza' ELSE 'Lácteos' END,
       (i % 500) * 100 + 99,
       100
FROM n;

-- 100 clientes
;WITH n AS (SELECT TOP (100) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS i FROM sys.all_objects)
INSERT INTO Clientes (Id, Nombre, Email)
SELECT NEWID(), CONCAT('Cliente ', i), CONCAT('cliente', i, '@mail.com') FROM n;

-- 1.000 pedidos con 3 líneas cada uno
DECLARE @i INT = 1;
WHILE @i <= 1000
BEGIN
    DECLARE @pedidoId UNIQUEIDENTIFIER = NEWID();

    INSERT INTO Pedidos (Id, ClienteId, FechaAlta, Estado, Total)
    SELECT @pedidoId,
           (SELECT TOP 1 Id FROM Clientes ORDER BY NEWID()),
           DATEADD(DAY, -(@i % 90), SYSUTCDATETIME()),
           CASE WHEN @i % 5 = 0 THEN 'Borrador' ELSE 'Confirmado' END,
           0;

    INSERT INTO LineasPedido (Id, PedidoId, ProductoId, Cantidad, PrecioUnitario)
    SELECT TOP 3 NEWID(), @pedidoId, Id, 1 + ABS(CHECKSUM(NEWID())) % 5, Precio
    FROM Productos ORDER BY NEWID();

    UPDATE Pedidos
    SET Total = (SELECT SUM(Cantidad * PrecioUnitario) FROM LineasPedido WHERE PedidoId = @pedidoId)
    WHERE Id = @pedidoId;

    SET @i += 1;
END;
GO

-- Procedimiento almacenado para la 4.5
CREATE OR ALTER PROCEDURE ActualizarStock
    @ProductoId UNIQUEIDENTIFIER,
    @Cantidad INT
AS
    UPDATE Productos SET Stock = Stock - @Cantidad WHERE Id = @ProductoId;
GO
```
> El bucle de pedidos usa `ORDER BY NEWID()` sobre 50.000 productos: tarda unos segundos, y es solo para cargar datos de prueba.

---

## Cómo pedírselo a un asistente de IA

Con este archivo en `docs/clase4/INSTRUCCIONES.md`, alcanza con decirle:

> Seguí `docs/clase4/INSTRUCCIONES.md`. Hacé las tareas en orden, de a una, respetando las reglas generales. Al terminar cada tarea, corré `dotnet build`, verificá el criterio de aceptación y listame los archivos que creaste o modificaste antes de pasar a la siguiente.
