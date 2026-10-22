# CQRS y MediatR

> Las escrituras (commands) y las lecturas (queries) se separan. Todo caso de uso pasa por MediatR con un pipeline de logging y validación.

## Commands y queries

| | Command | Query |
|---|---|---|
| Qué hace | Cambia datos | Solo lee |
| Accede por | `I[Entidad]Repository` (carga y guarda la entidad) | `I[Entidad]Consultas` (proyecta directo al DTO) |
| EF Core | con tracking | `AsNoTracking`, `Select` al DTO |
| Devuelve | id, nada o el resultado mínimo | un DTO o `PagedResult<T>` |

Cada caso de uso es una carpeta con su `Command`/`Query`, su `Handler` y su `Validator`, por ejemplo `Pedidos/Commands/ConfirmarPedido`.

## Pipeline (behaviors)

Se registran en `ApplicationServiceExtensions`; el primero envuelve a los siguientes:

1. `LoggingBehavior`: registra cada solicitud.
2. `ValidationBehavior`: ejecuta los validadores de FluentValidation y lanza `ValidationException` si fallan.

Así el handler nunca recibe datos inválidos y el controlador no valida nada.

## Reglas

- **MediatR queda en 12.5.0**: es la última versión con licencia Apache 2.0 (la 13+ exige clave de licencia). No subirla sin decidirlo.
- Los controladores solo conocen `ISender` o `IMediator`.
- Los listados devuelven `PagedResult<T>` con `pagina`, `tamanio` y `total`, y el `tamanio` máximo es 100.
- Una query no modifica nada. Si necesitás cambiar datos, es un command.
- Los DTO solo transportan datos: no llevan lógica.

## Dónde mirar un ejemplo completo

`PedidosSystem/src/Pedidos.Application/Pedidos/Queries/ListarPedidos` (query con validador y `PagedResult`) y `.../Commands/ConfirmarPedido` (command).
