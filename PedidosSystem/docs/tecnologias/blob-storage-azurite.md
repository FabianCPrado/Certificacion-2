# Blob Storage (Azurite) · imágenes de productos

## Para qué se usa
Guardar las **imágenes de los productos** como archivos. La base de datos no guarda las imágenes. En local se usa **Azurite**, el emulador de Azure Storage.

## Resumen de la configuración

| Dato | Valor |
|---|---|
| Imagen Docker | `mcr.microsoft.com/azure-storage/azurite` |
| Puertos | `10000` Blob · `10001` Queue · `10002` Table (solo se usa Blob) |
| Volumen | `azurite-data` → `/data` |
| Paquete NuGet | `Azure.Storage.Blobs` 12.30.1 |
| Clave en `appsettings.Development.json` | `ConnectionStrings:Storage` |
| Contenedor | `productos` |
| Visor | Extensión *Azure Storage* de VS Code o Azure Storage Explorer |

## Docker (`docker-compose.yml`)
```yaml
azurite:
  image: mcr.microsoft.com/azure-storage/azurite
  command: azurite --skipApiVersionCheck --blobHost 0.0.0.0 --queueHost 0.0.0.0 --tableHost 0.0.0.0 --location /data
  ports:
    - "10000:10000"
    - "10001:10001"
    - "10002:10002"
  volumes:
    - azurite-data:/data
```

| Parte del comando | Por qué |
|---|---|
| `--skipApiVersionCheck` | El SDK de Azure usa una versión de API más nueva que la que conoce Azurite; sin esto responde 400 (`The API version … is not supported by Azurite`) |
| `--blobHost 0.0.0.0` (y queue, table) | Escuchar en todas las interfaces del contenedor para que el puerto publicado funcione |
| `--location /data` | Dónde guarda los datos; es el volumen `azurite-data` |

## Cadena de conexión
```json
"Storage": "UseDevelopmentStorage=true"
```
Es un atajo del SDK que equivale a:
```
DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;
```
La cuenta `devstoreaccount1` y su clave son **públicas y fijas** (las mismas para todos los emuladores): no son un secreto.

## Registro en el código
[`InfrastructureServiceExtensions.cs`](../../src/Pedidos.Infrastructure/InfrastructureServiceExtensions.cs)
```csharp
services.AddSingleton(new BlobServiceClient(configuration.GetConnectionString("Storage") ?? "UseDevelopmentStorage=true"));
services.AddSingleton<IImagenesStorage, BlobImagenesStorage>();
```
`BlobServiceClient` es **Singleton**: uno para toda la aplicación.

## Cómo se guardan las imágenes
- **Contenedor `productos`**, creado la primera vez con `PublicAccessType.Blob`: cada archivo se puede leer con su URL, pero **no se pueden listar** los archivos sin credenciales.
- **Nombre del blob: `{productoId}/{archivo}`**, por ejemplo `98571078-…/bebidas-estante.jpg`. El `/` es parte del nombre; los exploradores lo muestran como una carpeta por producto.
- **Buscar** las imágenes de un producto = listar los blobs con el prefijo `{productoId}/`.
- Un archivo con otro nombre se **agrega** al producto; con el mismo nombre, **reemplaza** al anterior.
- Se guarda el `Content-Type` del archivo, para que el navegador lo muestre como imagen.

La URL de una imagen tiene la forma:
```
http://127.0.0.1:10000/devstoreaccount1/productos/{productoId}/{archivo}
```

## Dónde está el código

| Qué | Dónde |
|---|---|
| Interfaz (Application) | [`IImagenesStorage`](../../src/Pedidos.Application/Interfaces/Storage/IImagenesStorage.cs) |
| Implementación | [`BlobImagenesStorage`](../../src/Pedidos.Infrastructure/Storage/BlobImagenesStorage.cs) |
| Endpoints | [`DemoClase5Endpoints`](../../src/Pedidos.Api/Endpoints/DemoClase5Endpoints.cs) (`MapBlob`) |
| Fotos de ejemplo | [`ejemplos/fotos/`](../../ejemplos/fotos) y su [atribución](../../ejemplos/fotos/ATRIBUCIONES.md) |

## Endpoints (solo Development)

| Método | Ruta | Qué hace |
|---|---|---|
| POST | `/demo/clase5/imagenes/{productoId}` | Agrega una imagen (campo `archivo`). 404 si el producto no existe, 400 si no es una imagen |
| GET | `/demo/clase5/imagenes/{productoId}` | Devuelve todas las imágenes del producto (`cantidad` e `imagenes` con `nombre` y `url`) |

El producto se valida contra SQL Server antes de subir o buscar.

## Cómo verlo
- **Navegador:** abrir la `url` que devuelve la API.
- **VS Code:** extensión *Azure Storage* → *Attach Storage Account* → `UseDevelopmentStorage=true` → *Blob Containers → productos*.
- **Azure Storage Explorer:** *Emulator - Default Ports → Blob Containers → productos*.

## Empezar de cero
Borrar todas las imágenes de prueba (reinicia Azurite con su volumen limpio):
```powershell
docker compose rm -sf azurite
docker volume rm pedidossystem_azurite-data
docker compose up -d azurite
```

## Problemas frecuentes

| Síntoma | Causa y solución |
|---|---|
| POST responde 500 con `The API version … is not supported by Azurite` | Falta `--skipApiVersionCheck` en el comando de Azurite |
| La URL no abre | Azurite no está corriendo, o el contenedor `productos` aún no existe (se crea con la primera imagen) |
| No se ven las imágenes en VS Code | Probar *Reload Window* y volver a adjuntar la cuenta con la cadena completa; si falla, usar Azure Storage Explorer |
| Un producto con muchas fotos tarda en listar | Normal en cientos de blobs; si crece, guardar un índice de imágenes en una base |

## Pasar a Azure Storage
Cambiar `ConnectionStrings:Storage` por la cadena de la cuenta de almacenamiento (en *user secrets*). Para un contenedor privado, no usar `PublicAccessType.Blob` y entregar las imágenes con una **SAS** (firma de acceso compartido). El código de `IImagenesStorage` no cambia.
