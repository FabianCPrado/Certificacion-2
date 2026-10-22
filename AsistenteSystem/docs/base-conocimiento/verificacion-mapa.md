# Verificación del mapa de herramientas

`src/Asistente.Api/mapa-sistema.json` contrastado con el código de cada servicio (2026-10-22).

## Por herramienta

| Herramienta | Ruta en el mapa | ¿Existe? | Observación |
|---|---|---|---|
| `ObtenerPrecioYStock` | `GET /api/productos/{categoria}/{id}` | Sí | `asistente-service` lee con la policy `LecturaCatalogo` (escribir da 403) |
| `BuscarProductos` | `GET /api/productos/buscar?categoria=&nombre=` | Sí | `categoria` y `nombre` son obligatorios |
| `UltimosPedidosDeCliente` | `GET /api/pedidos?clienteId=&pagina=1&tamanio=` | Sí | **La descripción dice "con sus líneas" y el resumen no las trae** |
| `EstadoDePedido` | `GET /api/pedidos/{id}` | Sí | La respuesta incluye `EmailCliente`: hay que filtrarlo |
| `DatosDeCliente` | `GET /api/clientes/{id}` | Sí | La respuesta incluye `Email`: hay que filtrarlo |

Las cinco rutas existen. El prerrequisito "agregar `GET /api/pedidos?clienteId=` en Pedidos" de la especificación **ya no hace falta**: el endpoint existe, con `pagina` y `tamanio` en lugar de `top`.

## Diferencias respecto a la especificación

| Tema | La especificación dice | El código real |
|---|---|---|
| Búsqueda | `GET /api/productos?categoria=&nombre=` | `GET /api/productos/buscar?...` (el mapa ya lo corrige) |
| Últimos pedidos | `?clienteId=&top=` | `?clienteId=&pagina=&tamanio=` (el mapa ya lo corrige) |
| Puerto de Clientes | `<puerto de ClientesSystem>` | 7201 (`appsettings.json` ya lo tiene) |
| Pedidos sin JWT | "decisión pendiente" | Confirmado: ni Pedidos ni Clientes exigen token |
| Alta de `asistente-service` | tarea 0 | Confirmado: no existe; el inicializador solo crea `pedidos-service` |
| Policy de Catálogo | ampliar a `asistente-service` en lectura | Confirmado: una sola policy `PedidosService` para **todo** el grupo, lectura y escritura |

## Pendientes que salen de la verificación

1. **Apuntar `UltimosPedidosDeCliente` al nuevo `GET /api/pedidos/detalle`** en el mapa (trae líneas, sin email).
2. ~~Categoría de un producto~~ **Resuelto:** `LineaPedidoDto` ahora incluye `Categoria` (el dominio ya la guardaba).
3. **Datos personales:** los DTOs reales traen email; los puertos de Application deben devolver tipos propios sin ese campo.
4. **Policy en Catálogo:** separar lectura (aceptar `asistente-service`) de escritura (solo `pedidos-service`).
5. ~~Carga del RAG~~ **Resuelto:** `GET /api/productos/categorias` y `GET /api/productos/todos`.

## Endpoints agregados para el asistente

| Sistema | Endpoint | Cambio |
|---|---|---|
| Catálogo | `GET /api/productos/categorias` | nuevo |
| Catálogo | `GET /api/productos/todos?pagina=&tamanio=` | nuevo |
| Pedidos | `GET /api/pedidos/detalle?clienteId=&pagina=&tamanio=` | nuevo |
| Pedidos | `LineaPedidoDto.Categoria` | campo agregado |
| Clientes | `GET /api/clientes?pagina=&tamanio=` | ya existía; sin cambios |
