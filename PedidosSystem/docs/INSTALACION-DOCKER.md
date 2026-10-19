# Instalación paso a paso: Docker Desktop, Docker Compose, Redis, MongoDB, Cosmos DB y Blob Storage

Guía para dejar listos en Windows los servicios que usa **PedidosSystem**. Con un solo archivo, [`docker-compose.yml`](../docker-compose.yml), se levantan todos:

| Servicio | Para qué sirve en el proyecto | Puerto(s) | Interfaz web |
|---|---|---|---|
| Redis | Caché de pedidos y productos | 6379 | Redis Insight: http://localhost:5540 |
| MongoDB | Auditoría de POST, PUT y DELETE | 27017 | Mongo Express: http://localhost:8081 |
| Cosmos DB (emulador) | Catálogo y valoraciones | 8083 (https) | Data Explorer: http://localhost:1234 |
| Blob Storage (Azurite) | Imágenes de productos | 10000 (Blob), 10001 (Queue), 10002 (Table) | — |

SQL Server **no** va en Docker: el proyecto usa LocalDB (viene con Visual Studio).

---

## 1. Instalar Docker Desktop

Docker Desktop ya incluye **Docker Compose**: no hay que instalarlo aparte.

### Requisitos
- Windows 10 (64 bits, versión 22H2 o superior) o Windows 11.
- Virtualización activada en la BIOS (casi siempre lo está).
- Unos 10 GB libres para las imágenes de este proyecto (Cosmos es la más pesada).

### Pasos
1. Descarga **Docker Desktop para Windows** desde https://www.docker.com/products/docker-desktop.
2. Ejecuta el instalador. Deja marcada la opción **Use WSL 2 instead of Hyper-V** (en Windows 10 Home es la única opción).
3. Cuando termine, **reinicia el equipo** si te lo pide.
4. Abre Docker Desktop y espera a que el icono de la ballena, abajo a la izquierda, quede en **verde** («Engine running»). Acepta los términos si los muestra; no hace falta crear cuenta.
5. Abre PowerShell y comprueba:

```powershell
docker --version
docker compose version
```

Los dos comandos deben mostrar una versión. Si PowerShell dice que `docker` no se reconoce, **cierra y abre la terminal** (o reinicia Visual Studio): se abrió antes de instalar y no tiene Docker en el PATH.

> Se escribe `docker compose` (con espacio). El comando viejo `docker-compose` (con guion) ya no se usa.

---

## 2. Levantar todos los servicios

```powershell
cd PedidosSystem
docker compose up -d
```

- La primera vez descarga las imágenes (varios minutos; depende de tu conexión).
- `-d` deja los contenedores corriendo en segundo plano.

Comprueba que están arriba:

```powershell
docker compose ps
```

Deben aparecer 6 contenedores en estado `running` (`Up`): `redis`, `redisinsight`, `mongo`, `mongo-express`, `azurite` y `cosmos`.

Para levantar solo uno, por ejemplo Redis: `docker compose up -d redis`.

---

## 3. Redis

**Imagen:** `redis:7-alpine` · **Puerto:** 6379 · **Cadena en la app:** `ConnectionStrings:Redis` = `localhost:6379,abortConnect=false,...` (en `appsettings.Development.json`, ya configurada).

### Comprobar que funciona
```powershell
docker exec -it pedidossystem-redis-1 redis-cli ping
```
Debe responder `PONG`.

### Ver las claves con Redis Insight
1. Abre http://localhost:5540.
2. Pulsa **Add Redis database**.
3. En **Host** escribe `redis` (no `localhost`: Insight corre dentro de Docker y ve a Redis por su nombre) y en **Port** `6379`. Guarda.
4. Después de llamar a `GET /api/cache/pedidos`, aparece una clave `pedidos:v1:...` con su tiempo de expiración (TTL).

> Si Redis está apagado la API **no falla**: lo registra en el log y responde desde SQL.

---

## 4. MongoDB

**Imagen:** `mongo:7` · **Puerto:** 27017 · **Cadena en la app:** `ConnectionStrings:Mongo` = `mongodb://localhost:27017/PedidosDb` · **Datos:** persisten en el volumen `mongo-data`.

### Comprobar que funciona
```powershell
docker exec -it pedidossystem-mongo-1 mongosh --eval "db.runCommand({ ping: 1 })"
```
Debe responder `{ ok: 1 }`.

### Ver la auditoría con Mongo Express
1. Haz cualquier `POST`, `PUT` o `DELETE` contra Pedidos (por ejemplo crear un pedido).
2. Abre http://localhost:8081 (sin usuario ni contraseña).
3. Entra a la base **PedidosDb** y a la colección **auditoria**: ahí está el registro.

La base y la colección se crean solas con la primera escritura.

---

## 5. Cosmos DB (emulador)

**Imagen:** `mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:vnext-latest` · **Puerto:** 8083 (HTTPS) y 1234 (Data Explorer) · **Datos:** volumen `cosmos-data`.

La cadena de conexión (`ConnectionStrings:Cosmos`) usa `https://localhost:8083/` y la **clave pública del emulador**, que es la misma para todos y no es un secreto. La app acepta el certificado autofirmado del emulador solo en desarrollo.

### Comprobar que funciona
1. Espera uno o dos minutos después del `up`: es el contenedor que más tarda en arrancar.
2. Abre http://localhost:1234 (Data Explorer).
3. Arranca **Pedidos**: crea solo la base y los contenedores (`CosmosInicializador`). Después aparecen en el Explorer.
4. Prueba en Swagger, apartado **Cosmos DB**: `POST /api/cosmos/catalogo/seed` carga el catálogo y `POST /api/cosmos/valoraciones` guarda una valoración (ejemplos en `ejemplos/cosmos-01-...json`).

### Cosas a saber
- El emulador **siempre informa 1 RU** (unidad de costo): la comparación real de costos solo se ve en Azure.
- Si el contenedor se reinicia en bucle, mira los registros: `docker compose logs cosmos`.
- Es el servicio que más falla en equipos con poca memoria o con la virtualización mal configurada.

---

## 6. Blob Storage (Azurite)

**Imagen:** `mcr.microsoft.com/azure-storage/azurite` · **Puertos:** 10000 (Blob), 10001 (Queue), 10002 (Table) · **Cadena en la app:** `ConnectionStrings:Storage` = `UseDevelopmentStorage=true` · **Datos:** volumen `azurite-data`.

Azurite es el emulador local de Azure Storage. El proyecto solo usa **Blob**: las imágenes de los productos van al contenedor `productos`, con el nombre `{productoId}/{archivo}`.

### Comprobar que funciona
```powershell
docker compose logs azurite
```
Debe decir que el servicio Blob está escuchando en `0.0.0.0:10000`.

### Probar una subida
1. En Swagger de Pedidos, apartado **Blob Storage**, usa `POST /api/blob/imagenes/{productoId}`.
2. Elige un archivo de `ejemplos/fotos` y un Id de producto de `ejemplos/ids-productos.md`.
3. Comprueba con `GET /api/blob/imagenes/{productoId}`.

El contenedor `productos` se crea solo en la primera subida.

> El comando de Azurite lleva `--skipApiVersionCheck` porque el SDK de Azure suele usar una versión de API más nueva que la que conoce el emulador. Si lo quitas, las subidas pueden fallar con un error de versión.

---

## 7. Comandos útiles

| Quiero… | Comando |
|---|---|
| Ver el estado | `docker compose ps` |
| Ver los registros de un servicio | `docker compose logs -f cosmos` |
| Apagar sin borrar datos | `docker compose stop` |
| Volver a encender | `docker compose start` |
| Apagar y quitar los contenedores (los datos se conservan) | `docker compose down` |
| Apagar y **borrar también los datos** de Mongo, Cosmos y Azurite | `docker compose down -v` |
| Actualizar las imágenes | `docker compose pull` y luego `docker compose up -d` |

---

## 8. Problemas frecuentes

| Síntoma | Causa y solución |
|---|---|
| `docker` no se reconoce | La terminal es anterior a la instalación. Ciérrala y abre otra. |
| `error during connect` o «Docker Desktop is unable to start» | Docker Desktop no está abierto o WSL 2 no está activo. Abre Docker Desktop y espera el icono verde. Si falla, en PowerShell como administrador: `wsl --update`. |
| `port is already allocated` | Otro programa usa ese puerto. Cierra el proceso que lo ocupa (`netstat -ano \| findstr :6379`) o cambia el puerto de la izquierda en `docker-compose.yml`. Ojo con el 8081 (Mongo Express): por eso Cosmos usa el 8083. |
| Cosmos tarda o no responde | Espera un par de minutos; revisa `docker compose logs cosmos`. Necesita memoria libre. |
| La API responde 503 en Clientes `api/redis/pedidos` | Pedidos no está corriendo. Levántalo primero. |
| El disco se llena | Las imágenes y los volúmenes de Docker ocupan espacio. Libera con `docker system prune` (borra contenedores parados e imágenes sin uso; **no** toca los volúmenes si no agregas `--volumes`). |

---

## 9. Siguiente paso

Con los contenedores arriba, vuelve al [README principal](../README.md) para crear las bases SQL y arrancar Pedidos y Clientes, y revisa [`clase5-bases-de-datos.md`](clase5-bases-de-datos.md) para el detalle de cada base.
