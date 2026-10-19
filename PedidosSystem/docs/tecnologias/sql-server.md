# SQL Server (LocalDB) · datos transaccionales

## Para qué se usa
Datos del negocio con transacciones: **pedidos, líneas de pedido, productos y clientes**. Pedidos sigue aquí porque el pedido, sus líneas y el descuento de stock deben guardarse juntos o no guardarse.

## Resumen de la configuración

| Dato | Valor |
|---|---|
| Servidor | `(localdb)\MSSQLLocalDB` (SQL Server LocalDB, sin Docker) |
| Base | `PedidosDb` |
| Autenticación | Windows (`Trusted_Connection=True`) |
| Paquetes NuGet | `Microsoft.EntityFrameworkCore.SqlServer` 10.0.12 y `Dapper` 2.1.66 |
| Clave en `appsettings.Development.json` | `ConnectionStrings:Pedidos` |

## Cadena de conexión
```json
"Pedidos": "Server=(localdb)\\MSSQLLocalDB;Database=PedidosDb;Trusted_Connection=True;TrustServerCertificate=True"
```

| Parte | Qué hace |
|---|---|
| `Server=(localdb)\MSSQLLocalDB` | Instancia LocalDB (en JSON la barra se escribe doble) |
| `Database=PedidosDb` | Nombre de la base |
| `Trusted_Connection=True` | Usa el usuario de Windows, sin contraseña |
| `TrustServerCertificate=True` | Acepta el certificado del servidor (solo local) |

Esta es la única cadena **obligatoria**: si falta, la API no arranca (`Falta la cadena de conexión 'Pedidos'`).

## Registro en el código
[`InfrastructureServiceExtensions.cs`](../../src/Pedidos.Infrastructure/InfrastructureServiceExtensions.cs)
```csharp
services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));   // Scoped
services.AddSingleton(new ConexionSql(connectionString));                                  // para Dapper
```
Repositorios y consultas de pedidos, clientes y productos son **Scoped**, igual que el `DbContext`.

## Tablas

| Tabla | Contenido |
|---|---|
| `Clientes` | Nombre y email |
| `Productos` | Nombre, categoría (índice), precio y stock |
| `Pedidos` | Cliente, email, dirección de envío (columnas), moneda, estado y fecha |
| `LineasPedido` | Líneas de cada pedido (FK con borrado en cascada), con precio y moneda |
| `Auditoria` | **Ya no recibe datos**: la auditoría está en MongoDB |

Procedimiento almacenado: `ActualizarStock(@ProductoId, @Cantidad)`, descuenta stock solo si alcanza.

## Crear la base y cargar datos
```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -i scripts\crear-base-de-datos.sql   # tablas, índices y procedimiento
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -d PedidosDb -i scripts\seed-clase4.sql   # 50.000 productos, 100 clientes, 1.000 pedidos
```
[`crear-base-de-datos.sql`](../../scripts/crear-base-de-datos.sql) se puede ejecutar varias veces sin romper nada.

## Dónde está el código

| Qué | Dónde |
|---|---|
| DbContext | [`AppDbContext`](../../src/Pedidos.Infrastructure/Persistence/AppDbContext.cs) |
| Mapeo de entidades | [`Persistence/MapeoEntidades/`](../../src/Pedidos.Infrastructure/Persistence/MapeoEntidades) |
| Escritura (reglas + persistencia) | [`Persistence/Repositories/`](../../src/Pedidos.Infrastructure/Persistence/Repositories) |
| Lectura (proyecciones) | [`Queries/`](../../src/Pedidos.Infrastructure/Queries) |
| Reportes con Dapper | [`PedidosReportes`](../../src/Pedidos.Infrastructure/Queries/Pedidos/PedidosReportes.cs) |

## Cómo verlo
SQL Server Management Studio o Azure Data Studio conectados al servidor **`(localdb)\MSSQLLocalDB`** con autenticación de Windows, base `PedidosDb`:
```sql
SELECT TOP 5 Id, Nombre, Categoria, Precio, Stock FROM PedidosDb.dbo.Productos;
```

## Problemas frecuentes

| Síntoma | Causa y solución |
|---|---|
| La API no encuentra un producto que se ve en la tabla | Se está mirando otra instancia de SQL Server (SQL Express, `localhost`…). La API usa `(localdb)\MSSQLLocalDB` |
| `Falta la cadena de conexión 'Pedidos'` | Falta `ConnectionStrings:Pedidos` en `appsettings.Development.json` |
| `sqlcmd` no se reconoce | No está instalado; usar SSMS o Azure Data Studio y ejecutar el script |
| Al instalar el paquete de EF Core para Cosmos, `FromSql` es ambiguo | Se llama explícitamente a `RelationalQueryableExtensions.FromSql(...)` |

## Pasar a SQL Server o Azure SQL
Cambiar solo `ConnectionStrings:Pedidos`. Con usuario y contraseña, guardarlos en *user secrets*, nunca en el repositorio.
