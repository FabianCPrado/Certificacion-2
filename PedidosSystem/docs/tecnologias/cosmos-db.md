# Cosmos DB · catálogo y valoraciones

## Para qué se usa
Base de documentos. Guarda el **catálogo** de productos (con el SDK y con EF Core) y las **valoraciones** de productos. Se trabaja contra el **emulador vNext** en Docker.

## Resumen de la configuración

| Dato | Valor |
|---|---|
| Imagen Docker | `mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:vnext-latest` |
| Puertos | `8083` (API) y `1234` (Data Explorer) |
| Protocolo | **HTTPS** con certificado autofirmado |
| Volumen | `cosmos-data` → `/data` |
| Paquetes NuGet | `Microsoft.Azure.Cosmos` 3.63.2, `Newtonsoft.Json` 13.0.4, `Microsoft.EntityFrameworkCore.Cosmos` 10.0.12 |
| Clave en `appsettings.Development.json` | `ConnectionStrings:Cosmos` |
| Base | `PedidosDb` |
| Visor | Data Explorer, `http://localhost:1234` |

## Docker (`docker-compose.yml`)
```yaml
cosmos:
  image: mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:vnext-latest
  ports:
    - "8083:8083"
    - "1234:1234"
  environment:
    PORT: "8083"
    PROTOCOL: https
    ENABLE_TELEMETRY: "false"
  volumes:
    - cosmos-data:/data
```

| Variable | Por qué |
|---|---|
| `PORT: "8083"` | El emulador usa el 8081 por defecto, pero ya lo ocupa Mongo Express. Cambiar `PORT` hace que el emulador anuncie ese puerto a los clientes |
| `PROTOCOL: https` | El SDK de .NET **no acepta HTTP** con este emulador |
| `ENABLE_TELEMETRY: "false"` | No enviar datos de uso a Microsoft |

## Cadena de conexión
```json
"Cosmos": "AccountEndpoint=https://localhost:8083/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="
```
La clave es la **clave pública y fija del emulador**: no es un secreto y el emulador no la valida. Con una cuenta real de Azure la clave sí es secreta y va en *user secrets*.

## Bases y contenedores

| Contenedor | Partition key | Quién lo crea | Contenido |
|---|---|---|---|
| `Productos` | `/categoria` | `CosmosInicializador` al arrancar (solo Development) | Catálogo con el SDK (`POST /demo/clase5/catalogo/seed`) |
| `ProductosEf` | `Categoria` (EF Core usa el nombre de la propiedad, en PascalCase) | `EnsureCreated` en `CosmosInicializador` | Catálogo con EF Core (`POST /demo/clase5/ef/seed`) |
| `valoraciones` | `/productoId` | Se crea la primera vez que se usa | Valoraciones de productos |

Si el emulador no está corriendo al arrancar, `CosmosInicializador` escribe una advertencia y la API arranca igual.

### El mismo producto en los dos contenedores
Mismos datos, distinto formato (verificado en el emulador):

```json
// Productos (SDK): camelCase, id elegido por nosotros
{ "id": "prod-1", "categoria": "Electro", "nombre": "Electro 1", "precio": 1001,
  "atributos": { "pulgadas": "55", "voltaje": "220V" } }

// ProductosEf (EF Core): PascalCase, id generado y campo "$type"
{ "id": "b1934ca8-7c4b-4947-8544-544defc7764d", "$type": "ProductoCatalogo",
  "Categoria": "Electro", "Nombre": "EF 1", "Precio": 501,
  "Atributos": { "pulgadas": "32" } }
```
(Se omiten los campos internos de Cosmos: `_rid`, `_etag`, `_ts`.) Por eso la demo usa **dos contenedores**: un mismo contenedor mezclaría los dos formatos.

## Cliente del SDK (Singleton)
[`CosmosOpciones.CrearCliente`](../../src/Pedidos.Infrastructure/Cosmos/CosmosOpciones.cs)

| Opción | Valor | Por qué |
|---|---|---|
| `ConnectionMode` | `Gateway` | El emulador solo soporta modo gateway |
| `HttpClientFactory` | acepta el certificado autofirmado **solo si el destino es `localhost`** | Contra una cuenta real de Azure la validación sigue activa |
| `UseSystemTextJsonSerializerWithOptions` | camelCase | `Id` se guarda como `id` (lo exige Cosmos) sin poner atributos de Cosmos en Domain |
| `RequestTimeout` | 10 segundos | Que no se cuelgue si el emulador no responde |

El `CosmosClient` es **Singleton**: uno solo para toda la aplicación. Crear uno por request agota las conexiones.

## Registro en el código
[`InfrastructureServiceExtensions.cs`](../../src/Pedidos.Infrastructure/InfrastructureServiceExtensions.cs)
```csharp
var cosmosConexion = configuration.GetConnectionString("Cosmos") ?? CosmosPorDefecto;
services.AddSingleton(_ => CosmosOpciones.CrearCliente(cosmosConexion));
services.AddSingleton<CosmosValoraciones>();
services.AddSingleton<IValoracionRepository, ValoracionCosmosRepository>();
services.AddSingleton<IValoracionConsultas, ValoracionCosmosConsultas>();
services.AddSingleton<ICatalogoRepository, CatalogoRepository>();

services.AddDbContext<CatalogoDbContext>(options =>
    options.UseCosmos(cosmosConexion, CosmosOpciones.BaseDatos, cosmos => cosmos
        .ConnectionMode(ConnectionMode.Gateway)
        .HttpClientFactory(CosmosOpciones.CrearHttpClient)));
```
En `Program.cs` (solo Development): `await app.Services.InicializarCosmosAsync();`

## Dónde está el código

| Qué | Dónde |
|---|---|
| Entidad (Domain) | [`ProductoCatalogo`](../../src/Pedidos.Domain/Catalogo/ProductoCatalogo.cs) |
| Interfaces (Application) | `ICatalogoRepository`, `IValoracionRepository`, `IValoracionConsultas`, `IDemoCatalogo`, `IDemoCatalogoEf` |
| Catálogo con el SDK | [`CatalogoRepository`](../../src/Pedidos.Infrastructure/Cosmos/CatalogoRepository.cs) y [`DemoCatalogo`](../../src/Pedidos.Infrastructure/Cosmos/DemoCatalogo.cs) |
| Catálogo con EF Core | [`CatalogoDbContext`](../../src/Pedidos.Infrastructure/Cosmos/CatalogoDbContext.cs) y [`DemoCatalogoEf`](../../src/Pedidos.Infrastructure/Cosmos/DemoCatalogoEf.cs) |
| Valoraciones | [`CosmosValoraciones`](../../src/Pedidos.Infrastructure/Cosmos/CosmosValoraciones.cs), [`ValoracionCosmosRepository`](../../src/Pedidos.Infrastructure/Cosmos/ValoracionCosmosRepository.cs), [`ValoracionCosmosConsultas`](../../src/Pedidos.Infrastructure/Cosmos/ValoracionCosmosConsultas.cs) |
| Creación de la base | [`CosmosInicializador`](../../src/Pedidos.Infrastructure/Cosmos/CosmosInicializador.cs) |

## Endpoints
Demo del catálogo (`/demo/clase5`, solo Development): `POST /catalogo/seed`, `GET /catalogo/{categoria}/{id}`, `GET /catalogo/bueno`, `GET /catalogo/malo` (**insegura a propósito**), `GET /catalogo/cruzada`, `GET /catalogo/repositorio/{categoria}`, `POST /ef/seed`, `GET /ef/{categoria}`.
Valoraciones: `POST /api/cosmos/valoraciones` y `GET /api/cosmos/valoraciones/{productoId}`.

## Cómo verlo
**Data Explorer:** `http://localhost:1234` (con `http`, no `https`) → `PedidosDb` → contenedor → *Items*. Si el navegador pide confiar en el certificado, abrir antes `https://localhost:8083` y aceptarlo.

## Limitaciones del emulador vNext

| Limitación | Consecuencia |
|---|---|
| No calcula RU: siempre devuelve **1** | La diferencia de costo entre el point read y la consulta cruzada solo se ve en Azure real |
| No valida la clave | Cualquier clave sirve |
| Solo modo gateway | No se puede usar modo directo |
| Índices de rango y compuestos: no disponibles | Las consultas funcionan, pero sin optimización |
| Bulk del SDK de .NET: no soportado | No usar `AllowBulkExecution` |
| EF Core: `AnyAsync` con `WithPartitionKey` falla (`Identifier 'root' could not be resolved`) | El seed de EF no comprueba existencia: cada llamada agrega 30 documentos nuevos |

## Problemas frecuentes

| Síntoma | Causa y solución |
|---|---|
| Error de certificado en el navegador | Abrir `https://localhost:8083` y aceptar el certificado |
| `The Newtonsoft.Json package must be explicitly referenced` | Falta la referencia a `Newtonsoft.Json`; está en `Directory.Packages.props` |
| `FromSql` ambiguo al instalar el paquete de EF para Cosmos | Se llama a `RelationalQueryableExtensions.FromSql(...)` explícito (en `ProductosConsultas.cs` y `DemoMalasPracticas.cs`) |
| Los endpoints de Cosmos responden 500 | El contenedor `cosmos` no está corriendo |
| Aviso "No se pudo inicializar Cosmos DB" al arrancar | Mismo motivo; la API arranca igual |

## Pasar a una cuenta de Azure
Cambiar `ConnectionStrings:Cosmos` por la cadena de la cuenta (en *user secrets*):
```
AccountEndpoint=https://<cuenta>.documents.azure.com:443/;AccountKey=<clave>;
```
Con una cuenta real: las RU sí se calculan, la autenticación se aplica y el certificado se valida normalmente. El código no cambia.
