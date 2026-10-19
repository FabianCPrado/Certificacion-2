# Scripts SQL

Scripts para crear las dos bases de datos del proyecto en **SQL Server LocalDB** (`(localdb)\MSSQLLocalDB`). Todos son **idempotentes**: se pueden ejecutar varias veces sin duplicar nada.

| Orden | Archivo | Qué hace | Base |
|---|---|---|---|
| 1 | [`01-PedidosDb-crear.sql`](01-PedidosDb-crear.sql) | Crea la base `PedidosDb` con sus tablas, claves e índices (`Clientes`, `Productos`, `Pedidos`, `LineasPedido`, `Auditoria`) y el procedimiento `ActualizarStock` | PedidosDb |
| 2 | [`02-PedidosDb-procedimientos.sql`](02-PedidosDb-procedimientos.sql) | Procedimientos almacenados de la clase 4: `ActualizarStock`, `CancelarBorradoresAntiguos` y `ObtenerVentasPorDia` | PedidosDb |
| 3 | [`03-PedidosDb-datos-de-prueba.sql`](03-PedidosDb-datos-de-prueba.sql) | Datos de prueba: 50.000 productos, 100 clientes y 1.000 pedidos con 3 líneas cada uno. Si ya hay productos, no hace nada | PedidosDb |
| 4 | [`04-ClientesDb-crear.sql`](04-ClientesDb-crear.sql) | Crea la base `ClientesDb` con la tabla `Clientes` y un índice único por email. Si `PedidosDb` ya tiene clientes, los copia con el mismo `Id` | ClientesDb |

El orden importa en dos puntos:
- El script 2 y el 3 **no** crean ni seleccionan la base: hay que ejecutarlos sobre `PedidosDb` (después del 1).
- El 4 va **al final** para que copie los 100 clientes que carga el 3.

Las copias de estos scripts también están en `PedidosSystem/scripts` y `ClientesSystem/scripts`. Si cambias uno, cámbialo en los dos lados.

---

## Cómo ejecutarlos

### Opción A: Visual Studio (la más simple, no instala nada)
1. Menú **Ver → Explorador de objetos de SQL Server**.
2. Despliega **SQL Server → (localdb)\MSSQLLocalDB**.
3. Abre cada script con **Archivo → Abrir → Archivo**, y en la barra del editor elige la conexión `(localdb)\MSSQLLocalDB`.
4. Para los scripts 2 y 3, elige en el desplegable de bases de datos **PedidosDb** (el script 1 ya la crea).
5. Pulsa **Ejecutar** (o `Ctrl+Shift+E`), en el orden de la tabla.

### Opción B: línea de comandos con `sqlcmd`
`sqlcmd` no viene instalado en Windows. Para instalarlo:

```powershell
winget install Microsoft.Sqlcmd
```

Cierra y abre la terminal, y desde esta carpeta (`sql`):

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -f 65001 -i 01-PedidosDb-crear.sql
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -f 65001 -d PedidosDb -i 02-PedidosDb-procedimientos.sql
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -f 65001 -d PedidosDb -i 03-PedidosDb-datos-de-prueba.sql
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -f 65001 -i 04-ClientesDb-crear.sql
```

`-E` usa tu usuario de Windows y `-f 65001` evita que se estropeen las tildes.

### Opción C: SSMS o Azure Data Studio
Conéctate al servidor `(localdb)\MSSQLLocalDB` con autenticación de Windows y ejecuta los scripts en orden, igual que en la opción A.

---

## Comprobar que quedó bien

```sql
SELECT COUNT(*) AS Productos FROM PedidosDb.dbo.Productos;   -- 50000 con los datos de prueba
SELECT COUNT(*) AS Clientes  FROM ClientesDb.dbo.Clientes;   -- 100 con los datos de prueba
```

Para empezar de cero, borra las bases y vuelve a ejecutar los scripts:

```sql
USE master;
DROP DATABASE IF EXISTS PedidosDb;
DROP DATABASE IF EXISTS ClientesDb;
```

> Esto **borra todos los datos** de las dos bases. Si Pedidos o Clientes están corriendo, deténlos antes: mantienen conexiones abiertas.

## Cadenas de conexión

Ya están en los `appsettings.Development.json` de cada API:

| Servicio | Cadena |
|---|---|
| Pedidos | `Server=(localdb)\MSSQLLocalDB;Database=PedidosDb;Trusted_Connection=True;TrustServerCertificate=True` |
| Clientes | `Server=(localdb)\MSSQLLocalDB;Database=ClientesDb;Trusted_Connection=True;TrustServerCertificate=True` |

> Redis, MongoDB, Cosmos DB y Blob Storage **no** usan estos scripts: se levantan con Docker. Ver [`PedidosSystem/docs/INSTALACION-DOCKER.md`](../PedidosSystem/docs/INSTALACION-DOCKER.md).
