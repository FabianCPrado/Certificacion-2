# Catálogo · :7301

Fuente: `CatalogoSystem/src/Catalogo.Api/Endpoints/ProductosEndpoints.cs`, `ProductoDto.cs` y `Program.cs`.

## Seguridad

Hay dos policies, ambas exigen `client_type = service`:

| Policy | Acepta `client_id` | Se aplica a |
|---|---|---|
| `LecturaCatalogo` | `pedidos-service` y `asistente-service` | Todos los `GET` |
| `PedidosService` | solo `pedidos-service` | Todas las escrituras (`POST`, `PUT`, `DELETE`) |

Un token de `asistente-service` lee con 200 y recibe **403** si intenta escribir (verificado). Sin token: 401.

## Endpoints que usa el asistente (solo lectura)

| Herramienta | Ruta | Parámetros | Respuestas |
|---|---|---|---|
| `ObtenerPrecioYStock` | `GET /api/productos/{categoria}/{id}` | ruta | `200 ProductoDto`, `404` |
| `BuscarProductos` | `GET /api/productos/buscar` | query `categoria`, `nombre` (ambos obligatorios) | `200 ProductoDto[]` |
| (carga del RAG) | `GET /api/productos/categorias` | — | `200 string[]` (categorías con productos) |
| (carga del RAG) | `GET /api/productos/todos` | query `pagina` (1), `tamanio` (100, máx. 100) | `200 ProductoDto[]`, ordenado por categoría, nombre e id |
| (por categoría) | `GET /api/productos/` | query `categoria`, `pagina` (por defecto 1; 20 por página) | `200 ProductoDto[]` |

Los resultados de listado y búsqueda salen ordenados por `Nombre`.

## ProductoDto

```csharp
record ProductoDto(string Id, string Categoria, string Nombre, decimal Precio, int Stock, IReadOnlyList<AtributoDto> Atributos);
record AtributoDto(string Parametro, string Valor);
```

La categoría y el id **juntos** identifican el producto. Para el campo `Fuente` del asistente: `("Catalogo", "{categoria}/{id}")`.

## Endpoints que el asistente NO usa (escritura)

`POST /`, `PUT /{categoria}/{id}`, `PUT /{categoria}/{id}/precio`, `DELETE /{categoria}/{id}`, `POST /{categoria}/{id}/stock/descontar`, `POST /{categoria}/{id}/stock/reponer`, `POST /{categoria}/aumentar-precio`.

## Para el RAG

Para cargar el catálogo completo basta recorrer `GET /api/productos/todos?pagina=N` hasta que una página venga vacía. `GET /api/productos/categorias` sirve para mostrar o validar las categorías. Ambos endpoints usan EF Core sobre SQL Server y quedan bajo la misma policy `PedidosService`.
