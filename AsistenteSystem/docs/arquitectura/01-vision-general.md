# Visión general

> Cinco sistemas independientes, cada uno con su base de datos, que se hablan por REST (y gRPC en un caso) y se autentican con JWT.

## Los sistemas

| Sistema | Qué hace | Base de datos | Puertos (https / http) |
|---|---|---|---|
| Authentication | Emite JWT para clientes de servicio | AuthenticationDB | 7401 / 5401 |
| Catalogo | Productos, precios y stock | CatalogoDb | 7301 / 5301 |
| Pedidos | Pedidos, líneas, auditoría, caché, imágenes, Cosmos | PedidosDb (+ Redis, Mongo, Azurite, Cosmos) | 7101 / 5101 |
| Clientes | Clientes | ClientesDb | 7201 / 5201 |
| Asistente | IA que responde consultando a los otros por sus APIs | Ninguna propia | 7501 / 5501 |

## Cómo se hablan

```text
Asistente ──JWT──► Authentication
    │                   ▲
    ├──Bearer──► Catalogo ◄──── Pedidos (REST + gRPC, con JWT)
    ├──────────► Pedidos  ◄──── Clientes (¿tiene pedidos?)
    └──────────► Clientes ◄──── Pedidos (datos del cliente)
```

- Cada sistema es dueño de sus datos. Nadie entra a la base de otro: se pide por su API.
- Pedidos guarda `ClienteId` como un campo, sin clave foránea, porque el cliente vive en otra base.
- Catalogo identifica un producto por **categoría + id**. Las líneas de pedido guardan las dos.
- Catalogo es el único que valida JWT hoy. Pedidos, Clientes y el Asistente no exigen token a quien los llama.

## Tecnología común

- .NET 10, ASP.NET Core, EF Core con SQL Server (LocalDB en desarrollo).
- Clean Architecture y CQRS con MediatR 12.5.0, FluentValidation, `ProblemDetails` para los errores.
- Pedidos y Clientes usan controladores; Authentication, Catalogo y Asistente usan Minimal APIs.

## Dónde seguir leyendo

- Cómo se organiza cada sistema por dentro: `02-capas-y-dependencias`.
- Qué rompo si cambio algo: `GET /api/sistemas/{nombre}/impacto`.
