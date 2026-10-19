# CatalogoSystem

Microservicio **Catálogo** (Clase 6): Minimal APIs + gRPC sobre Cosmos DB.

```
Front / Swagger ──REST──▶ Catalogo.Api (Minimal APIs)  ─┐
                                                        ├──▶ mismo caso de uso (Application) ──▶ Cosmos DB
Otros servicios ──gRPC──▶ Catalogo.Api (servicio gRPC) ─┘
```

## Capas
- `Catalogo.Domain`: entidades, solo datos.
- `Catalogo.Application`: casos de uso con MediatR, behaviors, validadores e interfaces.
- `Catalogo.Infrastructure`: Cosmos DB.
- `Catalogo.Api`: Minimal APIs y servicio gRPC (sin lógica).

El contrato gRPC está en `protos/catalogo.proto`, compartido con los clientes.

## Ejecutar
Requiere el emulador de Cosmos DB (el mismo de PedidosSystem: `docker compose up -d` allí). Lee el contenedor `Productos` de la base `PedidosDb`.

```bash
dotnet run --project src/Catalogo.Api
```

- Swagger: https://localhost:7301/swagger
- OpenAPI: https://localhost:7301/openapi/v1.json
- gRPC: `https://localhost:7301` (`catalogo.CatalogoService`)

> El puerto es 7301 (no 7201) porque ClientesSystem ya usa 7201.

## Endpoints
| Verbo | Ruta | |
|---|---|---|
| GET | `/api/productos?categoria=&pagina=` | lista de a 20 |
| GET | `/api/productos/buscar?categoria=&nombre=` | búsqueda por nombre |
| GET | `/api/productos/{categoria}/{id}` | 200 / 404 |
| PUT | `/api/productos/{categoria}/{id}/precio` | 204 / 404 / 400 (precio ≤ 0) |

Todas las respuestas REST traen el header `X-Tiempo-ms`. `/demo/clase6/*` solo existe en Development.

Ver `docs/clase6/PROMPT-ENDPOINTS.md` para la plantilla de prompt y la revisión del endpoint generado con IA.
