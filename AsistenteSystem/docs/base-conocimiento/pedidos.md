# Pedidos · :7101

Fuente: `PedidosSystem/src/Pedidos.Api/Controllers/PedidosController.cs`, los DTOs de `Pedidos.Application` y `PedidosConsultas.cs`.

## Seguridad

`Program.cs` de Pedidos no configura autenticación para las llamadas entrantes: hoy responde sin token. Es la decisión pendiente 1 de la especificación.

## Endpoints que usa el asistente (solo lectura)

| Herramienta | Ruta | Parámetros | Respuestas |
|---|---|---|---|
| `UltimosPedidosDeCliente` | `GET /api/pedidos` | query `clienteId` (Guid, opcional), `pagina` (1), `tamanio` (20) | `200 PagedResult<PedidoResumenDto>`, `400` |
| `EstadoDePedido` | `GET /api/pedidos/{id:guid}` | ruta | `200 PedidoDto`, `404` |
| `UltimosPedidosDeCliente` (con líneas) | `GET /api/pedidos/detalle` | query `clienteId`, `pagina` (1), `tamanio` (20, máx. 100) | `200 PagedResult<PedidoDetalleDto>`, `400` |

```csharp
record PedidoDetalleDto(Guid Id, Guid ClienteId, string Estado, decimal Total, string Moneda, DateTime CreadoEn,
                        IReadOnlyList<LineaPedidoDto> Lineas);   // sin EmailCliente
```

El listado ya viene **ordenado por `CreadoEn` descendente**, así que `tamanio=5` da "los últimos 5".

## DTOs

```csharp
record PagedResult<T>(IReadOnlyList<T> Items, int Pagina, int Tamanio, int Total);

record PedidoResumenDto(Guid Id, Guid ClienteId, string Estado, decimal Total, string Moneda, DateTime CreadoEn);

record PedidoDto(Guid Id, Guid ClienteId, string EmailCliente, string Estado, decimal Total, string Moneda,
                 DateTime CreadoEn, IReadOnlyList<LineaPedidoDto> Lineas);
record LineaPedidoDto(string ProductoId, string Categoria, string NombreProducto, decimal PrecioUnitario, int Cantidad, decimal Subtotal);
```

Estados posibles: `Borrador`, `Confirmado`, `Cancelado`.

## Puntos que importan al asistente

- **El resumen no trae líneas.** Para "lo de siempre" se usa `GET /api/pedidos/detalle?clienteId=&pagina=1&tamanio=5`: una sola llamada, con líneas y sin email.
- **`LineaPedidoDto` ahora incluye `Categoria`**, así que se puede consultar el precio en Catálogo (`/{categoria}/{id}`) sin resolverla por otro camino.
- **`PedidoDto.EmailCliente` es dato personal.** La herramienta `EstadoDePedido` debe devolver al modelo solo estado, total, moneda y fecha.
- El pedido vive en `Borrador` hasta que una persona lo confirma: coincide con que el asistente solo propone.

## Endpoints que el asistente NO usa (escritura)

`POST /api/pedidos`, `PUT /{id}`, `DELETE /{id}`, `POST /{id}/confirmar`, `POST /{id}/cancelar`, `POST /cancelar-borradores-antiguos`.

Pedidos también expone `/api/clientes` y `/api/productos` como pasarela hacia los otros servicios; el asistente debe llamar a esos servicios directamente, no a través de Pedidos.
