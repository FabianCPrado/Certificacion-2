# AsistenteSystem

Servicio de IA de **Certificacion-2**. Recibe preguntas en lenguaje natural y las responde con un modelo **Claude**, **consultando a los otros servicios por sus APIs** (Catálogo, Pedidos, Clientes). Además tiene el apartado **Sistemas**: la estructura de cada sistema y una base de conocimiento de arquitectura. Puertos: **7501** (https) y **5501** (http).

## Reglas

1. Sin base de negocio propia: todo dato sale de las APIs de los otros servicios.
2. Solo lectura: ninguna herramienta modifica datos; los cambios los hace una persona en Pedidos.
3. No duplica reglas de negocio.
4. Identidad propia: cliente de servicio `asistente-service` en Authentication. Su JWT se pide solo cuando una herramienta lo necesita.
5. La IA es un detalle de `Infrastructure`; `Application` y `Domain` no la conocen.
6. Secretos (`Authentication:ClientSecret`, `IA:ApiKey`) solo en User Secrets.
7. El email del cliente nunca llega al modelo.

## Estructura

```
src/
  Asistente.Domain          Fuente, HerramientaInfo, SistemaInfo y tipos de la base de conocimiento
  Asistente.Application     IAsistente, puertos de consulta, queries + validators, behaviors (MediatR 12.5.0)
  Asistente.Infrastructure  IA (AsistenteIA, Herramientas), Servicios (consultas HTTP, TokenProvider, healthcheck), Mapa (lectura de archivos)
  Asistente.Api             Minimal APIs, GlobalExceptionHandler (ProblemDetails), mapa-sistema.json, sistemas.json
docs/
  base-conocimiento         Las APIs de los otros sistemas, verificadas contra el código
  arquitectura              Capas, CQRS, errores, seguridad, datos, pruebas
  instrucciones             Guías paso a paso (agregar un endpoint, cambiar un contrato, crear un sistema...)
tests/
  Asistente.Tests               herramientas, modelo falso, token, adaptadores HTTP, sistemas
  Asistente.IntegrationTests    WebApplicationFactory (nunca llama a Claude)
```

## Endpoints

| Método y ruta | Qué hace |
|---|---|
| `POST /api/asistente/preguntar` | Responde una pregunta con Claude y datos reales de los otros sistemas (400 si es inválida, 503 si falta la clave o un servicio no responde) |
| `GET /api/asistente/herramientas` | Lista el mapa: las 5 herramientas de solo lectura |
| `GET /api/asistente/salud` | Comprueba Authentication, Catálogo, Pedidos, Clientes y el modelo (200 o 503) |
| `GET /api/sistemas` | Lista los sistemas con puertos, base de datos y dependencias |
| `GET /api/sistemas/{nombre}` | La estructura completa de un sistema (capas, endpoints, contratos, tests, notas) |
| `GET /api/sistemas/{nombre}/impacto` | Qué sistemas lo consumen: qué se rompe si lo cambiás |
| `GET /api/sistemas/buscar?texto=` | Busca en la estructura, la arquitectura y las instrucciones |
| `GET /api/sistemas/arquitectura[/{id}]` | Documentos de arquitectura |
| `GET /api/sistemas/instrucciones[/{id}]` | Guías paso a paso |

La estructura de los sistemas sale de `src/Asistente.Api/sistemas.json` y los documentos de `docs/arquitectura` y `docs/instrucciones` (Markdown: se leen en el repo y también por la API). **Si cambiás un sistema, actualizá `sistemas.json`.** Una prueba vigila que los puertos coincidan con los `launchSettings.json`.

## Correrlo

```powershell
# Una vez: la clave de Claude (de platform.claude.com) y el secreto del cliente de servicio
cd AsistenteSystem\src\Asistente.Api
dotnet user-secrets set "IA:ApiKey" "sk-ant-..."

# Levantar, en este orden: Authentication, Catálogo, Pedidos, Clientes y por último el Asistente
dotnet run --project src/Asistente.Api --launch-profile https   # https://localhost:7501/swagger
dotnet test
```

Para probar: `Asistente.http` (extensión REST Client de VS Code) o el Swagger, cuyo cuerpo de `preguntar` ya trae un cliente de ejemplo. La guía completa está en `docs/instrucciones/06-levantar-y-probar-el-entorno.md`.

## Avance de la especificación

| Tarea | Estado |
|---|---|
| 0. Prerrequisitos (`asistente-service`, policy de lectura en Catálogo) | Hecha |
| 1. Esqueleto | Hecha |
| 2. Consultas HTTP con JWT (`TokenProvider`, `BearerTokenHandler`) | Hecha |
| 3. Preguntar con `IChatClient` (Claude) | Hecha |
| 4. RAG | No se hizo: el catálogo (41 productos) se consulta entero, sin embeddings |
| 5. Agente `proponer-pedido` | Descartado: se quitó el endpoint |
| 6. MCP | Opcional, pendiente |
| 7. Tests | Hechos (unitarios con modelo falso e integración) |

Las rutas de los otros sistemas, verificadas contra su código, están en `docs/base-conocimiento`.
