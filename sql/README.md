# Scripts SQL

Scripts para **crear desde cero** las cuatro bases de datos del proyecto en **SQL Server LocalDB** (`(localdb)\MSSQLLocalDB`). Todos son **idempotentes**: se pueden ejecutar varias veces sin duplicar nada.

| Orden | Archivo | Qué hace | Base |
|---|---|---|---|
| 1 | [`01-PedidosDb-crear.sql`](01-PedidosDb-crear.sql) | Crea la base `PedidosDb` con sus tablas, claves e índices (`Clientes`, `Pedidos`, `LineasPedido`, `Auditoria`). Los productos no están aquí: viven en `CatalogoDb` | PedidosDb |
| 2 | [`02-PedidosDb-procedimientos.sql`](02-PedidosDb-procedimientos.sql) | Procedimientos almacenados de la clase 4: `CancelarBorradoresAntiguos` y `ObtenerVentasPorDia` | PedidosDb |
| 3 | [`03-CatalogoDb-crear.sql`](03-CatalogoDb-crear.sql) | Crea la base `CatalogoDb` con `Productos` y `ProductoAtributos`, y carga 41 productos en 6 categorías (Abarrotes, Bebidas, Conservas, Galletas, Lacteos, Limpieza). 2 quedan sin stock y 3 con stock bajo. Los ids son estables | CatalogoDb |
| 4 | [`04-PedidosDb-datos-de-prueba.sql`](04-PedidosDb-datos-de-prueba.sql) | Datos de prueba: 100 clientes y 1.000 pedidos con 3 líneas cada uno. Las líneas usan productos reales de `CatalogoDb` (cada cliente tiene 5 favoritos que se repiten). Si ya hay clientes, no hace nada | PedidosDb |
| 5 | [`05-ClientesDb-crear.sql`](05-ClientesDb-crear.sql) | Crea la base `ClientesDb` con la tabla `Clientes` y un índice único por email. Si `PedidosDb` ya tiene clientes, los copia con el mismo `Id` | ClientesDb |
| 6 | [`06-AuthenticationDb-crear.sql`](06-AuthenticationDb-crear.sql) | Crea la base `AuthenticationDB` con la tabla `ServiceClients` (los clientes que piden JWT) y registra la migración de EF Core | AuthenticationDB |

El orden importa en tres puntos:
- El script 2 y el 4 **no** crean ni seleccionan la base: hay que ejecutarlos sobre `PedidosDb` (después del 1).
- El 4 necesita que el 3 ya haya creado `CatalogoDb`, porque toma los productos de ahí.
- El 5 va **después del 4** para que copie los 100 clientes que carga.

**Authentication y los JWT.** El script 6 crea la base y la tabla, pero **no** carga los clientes de servicio: su secreto se guarda hasheado (PBKDF2 de ASP.NET Core Identity) y ese hash no se puede calcular en T-SQL. La API los crea al arrancar en Development, a partir de User Secrets:

| User Secret (proyecto `Authentication.Api`) | Cliente de servicio |
|---|---|
| `ClientCredentials:PedidosSecret` | `pedidos-service` |
| `ClientCredentials:AsistenteSecret` | `asistente-service` |

La clave de firma (`Jwt:SigningKey`) también es un User Secret, no va en la base. Ver [`AuthenticationSystem/README.md`](../AuthenticationSystem/README.md).

Las copias de los scripts de Pedidos y Clientes también están en `PedidosSystem/scripts` y `ClientesSystem/scripts`. Si cambias uno, cámbialo en los dos lados.

---

## Cómo ejecutarlos

### Opción A: Visual Studio (la más simple, no instala nada)
1. Menú **Ver → Explorador de objetos de SQL Server**.
2. Despliega **SQL Server → (localdb)\MSSQLLocalDB**.
3. Abre cada script con **Archivo → Abrir → Archivo**, y en la barra del editor elige la conexión `(localdb)\MSSQLLocalDB`.
4. Para los scripts 2 y 4, elige en el desplegable de bases de datos **PedidosDb** (el script 1 ya la crea).
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
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -f 65001 -i 03-CatalogoDb-crear.sql
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -f 65001 -d PedidosDb -i 04-PedidosDb-datos-de-prueba.sql
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -f 65001 -i 05-ClientesDb-crear.sql
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -f 65001 -i 06-AuthenticationDb-crear.sql
```

`-E` usa tu usuario de Windows y `-f 65001` evita que se estropeen las tildes.

### Opción C: SSMS o Azure Data Studio
Conéctate al servidor `(localdb)\MSSQLLocalDB` con autenticación de Windows y ejecuta los scripts en orden, igual que en la opción A.

---

## Comprobar que quedó bien

```sql
SELECT COUNT(*) AS Pedidos   FROM PedidosDb.dbo.Pedidos;     -- 1000 con los datos de prueba
SELECT COUNT(*) AS Clientes  FROM ClientesDb.dbo.Clientes;   -- 100 con los datos de prueba
SELECT COUNT(*) AS Productos FROM CatalogoDb.dbo.Productos;  -- 41
```

Para empezar de cero, borra las bases y vuelve a ejecutar los scripts:

```sql
USE master;
DROP DATABASE IF EXISTS PedidosDb;
DROP DATABASE IF EXISTS ClientesDb;
DROP DATABASE IF EXISTS CatalogoDb;
DROP DATABASE IF EXISTS AuthenticationDB;
```

> Esto **borra todos los datos** de las cuatro bases. Si Pedidos o Clientes están corriendo, deténlos antes: mantienen conexiones abiertas.

## Cadenas de conexión

Ya están en los `appsettings.Development.json` de cada API:

| Servicio | Cadena |
|---|---|
| Pedidos | `Server=(localdb)\MSSQLLocalDB;Database=PedidosDb;Trusted_Connection=True;TrustServerCertificate=True` |
| Clientes | `Server=(localdb)\MSSQLLocalDB;Database=ClientesDb;Trusted_Connection=True;TrustServerCertificate=True` |
| Catálogo | `Server=(localdb)\MSSQLLocalDB;Database=CatalogoDb;Trusted_Connection=True;TrustServerCertificate=True` (en Development la API también puede crear la base sola) |
| Authentication | `Server=(localdb)\MSSQLLocalDB;Database=AuthenticationDB;Trusted_Connection=True;TrustServerCertificate=True` |

> Redis, MongoDB, Cosmos DB y Blob Storage **no** usan estos scripts: se levantan con Docker. Ver [`PedidosSystem/docs/INSTALACION-DOCKER.md`](../PedidosSystem/docs/INSTALACION-DOCKER.md).

