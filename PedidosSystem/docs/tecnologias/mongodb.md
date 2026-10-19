# MongoDB · auditoría

## Para qué se usa
Guardar **quién hizo qué** en cada POST, PUT y DELETE: método, ruta, código de respuesta, duración, usuario, traza y el cuerpo del request en JSON. Reemplaza a la tabla `Auditoria` de SQL Server.

## Resumen de la configuración

| Dato | Valor |
|---|---|
| Imagen Docker | `mongo:7` |
| Puerto | `27017` |
| Volumen | `mongo-data` → `/data/db` (los datos persisten) |
| Autenticación | ninguna (solo local) |
| Paquete NuGet | `MongoDB.Driver` 3.11.2 |
| Clave en `appsettings.Development.json` | `ConnectionStrings:Mongo` |
| Base / colección | `PedidosDb` / `auditoria` |
| Visor | Mongo Express, `http://localhost:8081` |

## Docker (`docker-compose.yml`)
```yaml
mongo:
  image: mongo:7
  ports:
    - "27017:27017"
  volumes:
    - mongo-data:/data/db

mongo-express:
  image: mongo-express:latest
  ports:
    - "8081:8081"
  environment:
    ME_CONFIG_MONGODB_URL: mongodb://mongo:27017/
    ME_CONFIG_BASICAUTH: "false"
  depends_on:
    - mongo
```
`ME_CONFIG_BASICAUTH: "false"` quita el usuario y la contraseña de Mongo Express: solo para uso local.

## Cadena de conexión
```json
"Mongo": "mongodb://localhost:27017/PedidosDb"
```
El texto después del último `/` es el **nombre de la base** (`PedidosDb`). Si falta, el código usa `PedidosDb` por defecto.

## Registro en el código
[`InfrastructureServiceExtensions.cs`](../../src/Pedidos.Infrastructure/InfrastructureServiceExtensions.cs)
```csharp
var mongoConexion = configuration.GetConnectionString("Mongo") ?? "mongodb://localhost:27017/PedidosDb";
services.AddSingleton(provider => new MongoAuditoriaColeccion(
    mongoConexion, provider.GetRequiredService<ILogger<MongoAuditoriaColeccion>>()));

services.AddSingleton<IAuditoriaRepository, AuditoriaMongoRepository>();
services.AddSingleton<IAuditoriaConsultas, AuditoriaMongoConsultas>();
```
Todo es **Singleton**: el cliente de MongoDB maneja su propio pool de conexiones y debe compartirse.

## Opciones importantes
En [`MongoAuditoriaColeccion`](../../src/Pedidos.Infrastructure/Mongo/MongoAuditoriaColeccion.cs):

| Opción | Valor | Por qué |
|---|---|---|
| `ServerSelectionTimeout` | 2 segundos | Si Mongo está caído, falla rápido. El valor por defecto es 30 s y la auditoría corre en cada POST, PUT y DELETE |
| Índices | `FechaUtc` (descendente) y `Ruta` | Equivalen a los de la tabla SQL; se crean al arrancar (`CreateMany` es idempotente) |

El middleware de auditoría captura cualquier error al guardar, así que **un fallo de Mongo nunca rompe la respuesta al cliente**.

## Dónde está el código

| Qué | Dónde |
|---|---|
| Interfaces (Application) | `IAuditoriaRepository`, `IAuditoriaConsultas` |
| Documento | [`AuditoriaDocumento`](../../src/Pedidos.Infrastructure/Mongo/AuditoriaDocumento.cs) |
| Cliente e índices | [`MongoAuditoriaColeccion`](../../src/Pedidos.Infrastructure/Mongo/MongoAuditoriaColeccion.cs) |
| Escritura | [`AuditoriaMongoRepository`](../../src/Pedidos.Infrastructure/Mongo/AuditoriaMongoRepository.cs) |
| Lectura | [`AuditoriaMongoConsultas`](../../src/Pedidos.Infrastructure/Mongo/AuditoriaMongoConsultas.cs) |
| Quién guarda | [`AuditoriaMiddleware`](../../src/Pedidos.Api/Middleware/AuditoriaMiddleware.cs) |
| Quién lee | `GET /api/auditoria` |

## Forma del documento
Los campos se guardan con el nombre de la propiedad en **PascalCase**: `Id` (GUID en formato *Standard*), `FechaUtc`, `Metodo`, `Ruta`, `EstadoHttp`, `DuracionMs`, `Usuario`, `TraceId`, `Request`. `Request` es un texto (el JSON del cuerpo, recortado a 4000 caracteres).

## Cómo verlo
- **Mongo Express:** `http://localhost:8081` → base `PedidosDb` → colección `auditoria`.
- **Consola:** `docker exec pedidossystem-mongo-1 mongosh --quiet PedidosDb --eval "db.auditoria.find().sort({FechaUtc:-1}).limit(5)"`.
- **API:** `GET /api/auditoria?ruta=/api/productos&tamanio=5`.

## Problemas frecuentes

| Síntoma | Causa y solución |
|---|---|
| Cada operación aparece dos veces | Con `http://localhost:5101` la redirección a HTTPS (307) también se audita. Usar `https://localhost:7101` |
| `/api/auditoria` no muestra el historial viejo | Los registros que estuvieran en la tabla SQL no se migraron |
| Mongo Express no arranca (`exec format error`) | Imagen descargada a medias (disco lleno). `docker rmi mongo-express` y volver a hacer `docker compose up -d` |
| `/api/auditoria` responde 500 | MongoDB no está corriendo; `docker compose up -d` |

## Pasar a MongoDB Atlas
Cambiar la cadena de conexión (en *user secrets*):
```
mongodb+srv://<usuario>:<clave>@<cluster>.mongodb.net/PedidosDb
```
El código no cambia.
