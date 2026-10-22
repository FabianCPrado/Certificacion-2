# Datos y comunicación entre sistemas

> Cada sistema es dueño de su base. Para obtener datos de otro, se llama a su API (REST, o gRPC en Catálogo).

## Datos

| Sistema | Base | Cómo se crea |
|---|---|---|
| Authentication | AuthenticationDB | Migraciones de EF Core, o `sql/06-AuthenticationDb-crear.sql` |
| Catalogo | CatalogoDb | `EnsureCreated` en Development, o `sql/03-CatalogoDb-crear.sql` |
| Pedidos | PedidosDb | `sql/01`, `02` y `04` (datos de prueba) |
| Clientes | ClientesDb | `sql/05-ClientesDb-crear.sql` |

- **No hay claves foráneas entre bases.** `Pedidos.ClienteId` es solo un valor; el cliente se pide a Clientes.
- **Los scripts de `sql/` crean desde cero y son idempotentes.** No son para actualizar una base existente.
- **El orden de los scripts importa:** el `04` (datos de Pedidos) necesita `CatalogoDb` ya creada.
- Redis, Mongo, Cosmos y Blob viven solo en Pedidos y solo en Infrastructure.

## Comunicación

- Cada llamada a otro sistema está detrás de una interfaz de Application (`ICatalogoConsultas`, `IClientesConsultas`...) con una implementación HTTP en Infrastructure.
- Se usan `HttpClient` con nombre creados por `IHttpClientFactory`, con un tiempo máximo.
- **El token se pide solo cuando hace falta** y se guarda en memoria hasta 1 minuto antes de que venza (`TokenProvider`). Nada se conecta al arrancar.
- **Un 404 del otro sistema es "no existe"** (devuelve `null`); cualquier otro fallo es "servicio no disponible" (503).
- **No se accede jamás a la base de otro sistema.** Para saber si una base responde, se consulta a la API que la usa.

## Contratos

Lo que un sistema expone y otro consume es un **contrato**. Cambiarlo sin avisar rompe al consumidor:

- `ProductoDto` de Catalogo lo usa Pedidos.
- `GET /api/pedidos?clienteId=` lo usa Clientes para saber si un cliente tiene pedidos.

Antes de cambiar uno, revisá qué sistemas lo consumen: cada contrato indica quién lo usa.
