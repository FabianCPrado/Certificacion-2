# AsistenteSystem

Servicio de IA de **Certificacion-2**. Recibe preguntas en lenguaje natural y las responde **consultando a los otros servicios por sus APIs** (Catálogo, Pedidos, Clientes). Puertos: **7501** (https) y **5501** (http).

## Reglas

1. Sin base de negocio propia: todo dato sale de las APIs de los otros servicios.
2. Solo lectura: lo que implique un cambio se devuelve como **propuesta**; lo confirma una persona en Pedidos.
3. No duplica reglas de negocio.
4. Identidad propia: cliente de servicio `asistente-service` en Authentication.
5. La IA es un detalle de `Infrastructure`; `Application` y `Domain` no la conocen.
6. Secretos (`Authentication:ClientSecret`, `IA:ApiKey`) solo en User Secrets.

## Estructura

```
src/
  Asistente.Domain          Fuente, HerramientaInfo
  Asistente.Application     IAsistente, IMapaSistema, queries + validators, behaviors (MediatR 12.5.0)
  Asistente.Infrastructure  MapaSistema (lee mapa-sistema.json), AsistenteNoConfigurado (marcador)
  Asistente.Api             Minimal APIs, GlobalExceptionHandler (ProblemDetails), mapa-sistema.json
tests/
  Asistente.Tests               validadores y mapa
  Asistente.IntegrationTests    WebApplicationFactory
```

## Endpoints

| Método y ruta | Estado |
|---|---|
| `GET /api/asistente/herramientas` | Funciona: lista el mapa (5 herramientas) |
| `POST /api/asistente/preguntar` | Valida (400); responde 503 hasta conectar el modelo (tarea 3) |
| `POST /api/asistente/proponer-pedido` | Valida (400); responde 503 hasta implementar el agente (tarea 5) |

## Correrlo

```powershell
cd AsistenteSystem
dotnet run --project src/Asistente.Api --launch-profile https   # https://localhost:7501/swagger
dotnet test
```

## Avance de la especificación

| Tarea | Estado |
|---|---|
| 0. Prerrequisitos (alta de `asistente-service`, policy de lectura en Catálogo) | Pendiente |
| 1. Esqueleto | Hecha |
| 2. Consultas HTTP con JWT (`TokenProvider`, `BearerTokenHandler`) | Parcial: solo `GET /herramientas` |
| 3. Preguntar (`IChatClient`) · 4. RAG · 5. Agente · 6. MCP · 7. Tests completos | Pendiente |

## Rutas reales verificadas

- Catálogo: `GET /api/productos/{categoria}/{id}` y `GET /api/productos/buscar`.
- Pedidos: `GET /api/pedidos?clienteId=&pagina=&tamanio=` (usa `tamanio`, no `top`).
- Clientes: `GET /api/clientes/{id}` (puerto 7201).
