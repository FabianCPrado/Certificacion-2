# Mapa del proyecto: recorrido guiado

> Cómo recorrer AsistenteSystem de punta a punta: qué hace cada archivo, por dónde pasa una pregunta y dónde cambiar cada cosa.

Los enlaces apuntan a archivos y líneas concretas. Los números de línea corresponden al código al escribir esta guía (2026-10-22): si el código cambia, pueden correrse unas líneas.

## Índice

1. [En un minuto](#1-en-un-minuto)
2. [Estructura de carpetas](#2-estructura-de-carpetas)
3. [Cada capa, archivo por archivo](#3-cada-capa-archivo-por-archivo)
4. [Recorrido 1: el camino de una pregunta](#4-recorrido-1-el-camino-de-una-pregunta)
5. [Recorrido 2: el apartado Sistemas](#5-recorrido-2-el-apartado-sistemas)
6. [Recorrido 3: el healthcheck](#6-recorrido-3-el-healthcheck)
7. [Recorrido 4: el arranque](#7-recorrido-4-el-arranque)
8. [Las dos IAs y sus herramientas](#8-las-dos-ias-y-sus-herramientas)
9. [Seguridad: dónde se aplica cada cosa](#9-seguridad-dónde-se-aplica-cada-cosa)
10. [Errores: de la excepción al código HTTP](#10-errores-de-la-excepción-al-código-http)
11. [Configuración, secretos y puertos](#11-configuración-secretos-y-puertos)
12. [Patrones de diseño y dónde están](#12-patrones-de-diseño-y-dónde-están)
13. [Pruebas](#13-pruebas)
14. [Datos, scripts y herramientas para probar](#14-datos-scripts-y-herramientas-para-probar)
15. [Paquetes y versiones](#15-paquetes-y-versiones)
16. [Si querés cambiar X, mirá Y](#16-si-querés-cambiar-x-mirá-y)
17. [Límites y pendientes](#17-límites-y-pendientes)
18. [Ruta de lectura sugerida](#18-ruta-de-lectura-sugerida)

---

## 1. En un minuto

**AsistenteSystem** es un servicio de IA para un ecosistema de microservicios (Authentication, Catálogo, Pedidos y Clientes). Recibe preguntas en lenguaje natural y las responde con un modelo **Claude**, que decide qué consultar. Es de **solo lectura**, no tiene base de datos propia y nunca entra a las bases de los otros sistemas: usa sus APIs.

| Endpoint | Para qué | Dónde está |
|---|---|---|
| `POST /api/asistente/preguntar` | Responde con datos reales de productos, pedidos y clientes | [AsistenteEndpoints.cs:14](../src/Asistente.Api/Endpoints/AsistenteEndpoints.cs#L14) |
| `POST /api/sistemas/preguntar` | Explica cómo está armado el ecosistema | [SistemasEndpoints.cs:15](../src/Asistente.Api/Endpoints/SistemasEndpoints.cs#L15) |
| `GET /api/asistente/herramientas` | Lista las herramientas que conoce | [AsistenteEndpoints.cs:16](../src/Asistente.Api/Endpoints/AsistenteEndpoints.cs#L16) |
| `GET /api/asistente/salud` | Comprueba que todo lo que necesita esté conectado | [AsistenteEndpoints.cs:19](../src/Asistente.Api/Endpoints/AsistenteEndpoints.cs#L19) |

```text
 Persona / Swagger / .http
            │ pregunta
            ▼
   ┌──────────────────┐   texto y resultados   ┌────────────────────┐
   │    Asistente     │◄──────────────────────►│ Claude (Anthropic) │
   └──┬───┬───┬───┬───┘                        │ elige qué consultar│
      │   │   │   │                            └────────────────────┘
      │   │   │   └──► Clientes  ──► ClientesDb
      │   │   └──────► Pedidos   ──► PedidosDb
      │   └──────────► Catálogo  ──► CatalogoDb   (con token)
      └──────────────► Authentication              (entrega el token)
```

---

## 2. Estructura de carpetas

```text
AsistenteSystem/
├── AsistenteSystem.slnx            la solución (6 proyectos)
├── Directory.Build.props           .NET 10, nullable, TreatWarningsAsErrors, SonarAnalyzer
├── Directory.Packages.props        versiones de todos los paquetes en un solo lugar
├── global.json                     versión del SDK de .NET
├── Asistente.http                  18 peticiones para probar a mano
├── README.md                       resumen del proyecto
├── .vscode/                        launch.json y tasks.json (F5 en VS Code; Git los ignora)
│
├── src/
│   ├── Asistente.Api/              la puerta de entrada (HTTP)
│   ├── Asistente.Application/      los casos de uso y los PUERTOS (interfaces)
│   ├── Asistente.Domain/           tipos simples
│   └── Asistente.Infrastructure/   la IA, los clientes HTTP, el token, el healthcheck
│
├── tests/
│   ├── Asistente.Tests/            69 pruebas unitarias
│   └── Asistente.IntegrationTests/ 10 pruebas contra el servidor real
│
└── docs/
    ├── MAPA-DEL-PROYECTO.md        esta guía
    ├── ESPECIFICACION-AsistenteSystem.md   la especificación original
    ├── arquitectura/               7 documentos (los lee la IA de Sistemas)
    ├── instrucciones/              5 guías paso a paso (las lee la IA de Sistemas)
    └── base-conocimiento/          las APIs de los otros sistemas (para personas)
```

**Regla de dependencias** (de afuera hacia adentro):

```text
Api ──► Application ──► Domain
 │           ▲
 └──► Infrastructure ──┘     Infrastructure implementa lo que Application define
```

---

## 3. Cada capa, archivo por archivo

### Asistente.Api: la puerta de entrada

| Archivo | Qué hace |
|---|---|
| [Program.cs](../src/Asistente.Api/Program.cs) | Arma la aplicación: registra capas ([12](../src/Asistente.Api/Program.cs#L12)), define los ejemplos de Swagger ([23](../src/Asistente.Api/Program.cs#L23)), el manejo de errores ([51](../src/Asistente.Api/Program.cs#L51)) y mapea los endpoints ([61](../src/Asistente.Api/Program.cs#L61)) |
| [Endpoints/AsistenteEndpoints.cs](../src/Asistente.Api/Endpoints/AsistenteEndpoints.cs) | `preguntar`, `herramientas` y `salud`. Solo reciben, mandan a MediatR y devuelven |
| [Endpoints/SistemasEndpoints.cs](../src/Asistente.Api/Endpoints/SistemasEndpoints.cs) | El único endpoint de Sistemas |
| [Infrastructure/GlobalExceptionHandler.cs](../src/Asistente.Api/Infrastructure/GlobalExceptionHandler.cs) | Convierte excepciones en `ProblemDetails` con su código HTTP |
| [mapa-sistema.json](../src/Asistente.Api/mapa-sistema.json) | Las 5 herramientas de negocio permitidas |
| [sistemas.json](../src/Asistente.Api/sistemas.json) | La estructura de los 4 sistemas (la lee la IA de Sistemas) |
| [appsettings.json](../src/Asistente.Api/appsettings.json) | Configuración sin secretos |
| [Properties/launchSettings.json](../src/Asistente.Api/Properties/launchSettings.json) | Puertos 7501 (https) y 5501 (http) |

### Asistente.Application: los casos de uso y los puertos

| Archivo | Qué hace |
|---|---|
| [Asistente/PreguntarQuery.cs](../src/Asistente.Application/Asistente/PreguntarQuery.cs) | La consulta ([7](../src/Asistente.Application/Asistente/PreguntarQuery.cs#L7)), su validador ([9](../src/Asistente.Application/Asistente/PreguntarQuery.cs#L9)) y su handler ([18](../src/Asistente.Application/Asistente/PreguntarQuery.cs#L18)) |
| [Asistente/ListarHerramientasQuery.cs](../src/Asistente.Application/Asistente/ListarHerramientasQuery.cs) | Devuelve el mapa de herramientas |
| [Asistente/VerificarSaludQuery.cs](../src/Asistente.Application/Asistente/VerificarSaludQuery.cs) | Delega el healthcheck en `IVerificadorSalud` |
| [Asistente/Dtos.cs](../src/Asistente.Application/Asistente/Dtos.cs) | `PreguntaRequest` ([5](../src/Asistente.Application/Asistente/Dtos.cs#L5)), `UsoDto` ([8](../src/Asistente.Application/Asistente/Dtos.cs#L8)), `RespuestaDto` ([10](../src/Asistente.Application/Asistente/Dtos.cs#L10)), `ComponenteSalud` y `SaludDto` |
| [Asistente/Consultas.cs](../src/Asistente.Application/Asistente/Consultas.cs) | Los tipos que vienen de otros sistemas: `ProductoInfo`, `LineaInfo`, `PedidoInfo`, `ClienteInfo` (sin email) |
| [Sistemas/PreguntarSobreSistemasQuery.cs](../src/Asistente.Application/Sistemas/PreguntarSobreSistemasQuery.cs) | Consulta, validador y handler del apartado Sistemas |
| [Sistemas/ConocimientoEcosistema.cs](../src/Asistente.Application/Sistemas/ConocimientoEcosistema.cs) | Las consultas sobre el ecosistema que la IA usa como herramientas: `Listar` ([34](../src/Asistente.Application/Sistemas/ConocimientoEcosistema.cs#L34)), `Obtener` ([40](../src/Asistente.Application/Sistemas/ConocimientoEcosistema.cs#L40)), `Impacto` ([45](../src/Asistente.Application/Sistemas/ConocimientoEcosistema.cs#L45)), `Buscar` ([64](../src/Asistente.Application/Sistemas/ConocimientoEcosistema.cs#L64)), `Documentos` ([101](../src/Asistente.Application/Sistemas/ConocimientoEcosistema.cs#L101)), `Documento` ([106](../src/Asistente.Application/Sistemas/ConocimientoEcosistema.cs#L106)) |
| [Interfaces/IAsistente.cs](../src/Asistente.Application/Interfaces/IAsistente.cs) | Puerto de la IA de negocio |
| [Interfaces/ISistemasAsistente.cs](../src/Asistente.Application/Interfaces/ISistemasAsistente.cs) | Puerto de la IA de Sistemas |
| [Interfaces/IConsultas.cs](../src/Asistente.Application/Interfaces/IConsultas.cs) | Puertos hacia los otros sistemas: `ICatalogoConsultas` ([8](../src/Asistente.Application/Interfaces/IConsultas.cs#L8)), `IPedidosConsultas` ([16](../src/Asistente.Application/Interfaces/IConsultas.cs#L16)), `IClientesConsultas` ([24](../src/Asistente.Application/Interfaces/IConsultas.cs#L24)) |
| [Interfaces/IConocimiento.cs](../src/Asistente.Application/Interfaces/IConocimiento.cs) | Puertos de la base de conocimiento: estructura y documentos |
| [Interfaces/IMapaSistema.cs](../src/Asistente.Application/Interfaces/IMapaSistema.cs), [IVerificadorSalud.cs](../src/Asistente.Application/Interfaces/IVerificadorSalud.cs) | Puertos del mapa de herramientas y del healthcheck |
| [Behaviors/LoggingBehavior.cs](../src/Asistente.Application/Behaviors/LoggingBehavior.cs), [ValidationBehavior.cs](../src/Asistente.Application/Behaviors/ValidationBehavior.cs) | Decoradores de MediatR: registran cada caso de uso y validan antes de ejecutarlo |
| [Exceptions/](../src/Asistente.Application/Exceptions) | `NotFoundException` (404) y `ServicioNoDisponibleException` (503) |
| [ApplicationServiceExtensions.cs](../src/Asistente.Application/ApplicationServiceExtensions.cs) | Registra MediatR, los behaviors ([17-18](../src/Asistente.Application/ApplicationServiceExtensions.cs#L17)), los validadores y `ConocimientoEcosistema` |

### Asistente.Domain: tipos simples

| Archivo | Qué hace |
|---|---|
| [Fuente.cs](../src/Asistente.Domain/Fuente.cs) | De dónde salió un dato: servicio y referencia |
| [HerramientaInfo.cs](../src/Asistente.Domain/HerramientaInfo.cs) | Una fila de `mapa-sistema.json` |
| [Sistemas.cs](../src/Asistente.Domain/Sistemas.cs) | `SistemaInfo` ([16](../src/Asistente.Domain/Sistemas.cs#L16)) y sus partes: endpoints, capas, dependencias, contratos, documentos |

### Asistente.Infrastructure: donde viven los detalles

| Archivo | Qué hace |
|---|---|
| [InfrastructureServiceExtensions.cs](../src/Asistente.Infrastructure/InfrastructureServiceExtensions.cs) | **El cableado**: valida el mapa ([22](../src/Asistente.Infrastructure/InfrastructureServiceExtensions.cs#L22)), registra los `HttpClient` ([34-37](../src/Asistente.Infrastructure/InfrastructureServiceExtensions.cs#L34)), elige entre Claude y el marcador "no configurado" ([54](../src/Asistente.Infrastructure/InfrastructureServiceExtensions.cs#L54)) y arma el cliente de IA con sus decoradores ([87-91](../src/Asistente.Infrastructure/InfrastructureServiceExtensions.cs#L87)) |
| [IA/ConversacionIA.cs](../src/Asistente.Infrastructure/IA/ConversacionIA.cs) | **El corazón**: una pregunta al modelo con sus herramientas ([17](../src/Asistente.Infrastructure/IA/ConversacionIA.cs#L17)), con límite de 60 s ([15](../src/Asistente.Infrastructure/IA/ConversacionIA.cs#L15)), consumo de tokens y traducción de fallos |
| [IA/AsistenteIA.cs](../src/Asistente.Infrastructure/IA/AsistenteIA.cs) | La IA de negocio: arma la pregunta y delega en `ConversacionIA` |
| [IA/SistemasIA.cs](../src/Asistente.Infrastructure/IA/SistemasIA.cs) | La IA de Sistemas, y su marcador `SistemasNoConfigurado` ([30](../src/Asistente.Infrastructure/IA/SistemasIA.cs#L30)) |
| [IA/Herramientas.cs](../src/Asistente.Infrastructure/IA/Herramientas.cs) | Las 5 herramientas de negocio ([65](../src/Asistente.Infrastructure/IA/Herramientas.cs#L65)), `Validar` contra el mapa ([52](../src/Asistente.Infrastructure/IA/Herramientas.cs#L52)) y `RegistroUso` ([10](../src/Asistente.Infrastructure/IA/Herramientas.cs#L10)) |
| [IA/HerramientasSistemas.cs](../src/Asistente.Infrastructure/IA/HerramientasSistemas.cs) | Las 6 herramientas de la IA de Sistemas ([22](../src/Asistente.Infrastructure/IA/HerramientasSistemas.cs#L22)) |
| [IA/Instrucciones.cs](../src/Asistente.Infrastructure/IA/Instrucciones.cs) | Los prompts de sistema: negocio ([6](../src/Asistente.Infrastructure/IA/Instrucciones.cs#L6)) y ecosistema ([22](../src/Asistente.Infrastructure/IA/Instrucciones.cs#L22)) |
| [IA/AsistenteNoConfigurado.cs](../src/Asistente.Infrastructure/IA/AsistenteNoConfigurado.cs) | Marcador cuando falta la clave: responde 503 con el motivo |
| [Servicios/TokenProvider.cs](../src/Asistente.Infrastructure/Servicios/TokenProvider.cs) | Pide el JWT a Authentication y lo guarda ([24](../src/Asistente.Infrastructure/Servicios/TokenProvider.cs#L24)) |
| [Servicios/BearerTokenHandler.cs](../src/Asistente.Infrastructure/Servicios/BearerTokenHandler.cs) | Agrega `Authorization: Bearer` a cada llamada a Catálogo ([11](../src/Asistente.Infrastructure/Servicios/BearerTokenHandler.cs#L11)) |
| [Servicios/ConsultasHttp.cs](../src/Asistente.Infrastructure/Servicios/ConsultasHttp.cs) | Los tres adaptadores HTTP: `CatalogoConsultasHttp` ([40](../src/Asistente.Infrastructure/Servicios/ConsultasHttp.cs#L40)), `PedidosConsultasHttp` ([80](../src/Asistente.Infrastructure/Servicios/ConsultasHttp.cs#L80)) y `ClientesConsultasHttp` ([98](../src/Asistente.Infrastructure/Servicios/ConsultasHttp.cs#L98)) |
| [Servicios/VerificadorSalud.cs](../src/Asistente.Infrastructure/Servicios/VerificadorSalud.cs) | El healthcheck de 6 componentes ([25](../src/Asistente.Infrastructure/Servicios/VerificadorSalud.cs#L25)) |
| [Mapa/MapaSistema.cs](../src/Asistente.Infrastructure/Mapa/MapaSistema.cs) | Lee `mapa-sistema.json` |
| [Mapa/EstructuraSistemas.cs](../src/Asistente.Infrastructure/Mapa/EstructuraSistemas.cs) | Lee `sistemas.json` |
| [Mapa/BibliotecaDocumentos.cs](../src/Asistente.Infrastructure/Mapa/BibliotecaDocumentos.cs) | Lee los `.md` de `docs/arquitectura` y `docs/instrucciones` |

---

## 4. Recorrido 1: el camino de una pregunta

Pregunta de ejemplo: `POST /api/asistente/preguntar` con *"¿Hay stock de yerba?"*.

| # | Qué pasa | Dónde |
|---|---|---|
| 1 | Llega la solicitud y se convierte en un `PreguntaRequest` | [AsistenteEndpoints.cs:34](../src/Asistente.Api/Endpoints/AsistenteEndpoints.cs#L34) |
| 2 | El endpoint envía un `PreguntarQuery` por MediatR, sin lógica propia | [AsistenteEndpoints.cs:35](../src/Asistente.Api/Endpoints/AsistenteEndpoints.cs#L35) |
| 3 | `LoggingBehavior` registra y `ValidationBehavior` valida: pregunta de 3 a 500 caracteres, `clienteId` no vacío. Si falla: `ValidationException`, que termina en **400** | [ValidationBehavior.cs:25](../src/Asistente.Application/Behaviors/ValidationBehavior.cs#L25), [PreguntarQuery.cs:13-14](../src/Asistente.Application/Asistente/PreguntarQuery.cs#L13) |
| 4 | El handler llama al puerto `IAsistente` | [PreguntarQuery.cs:18](../src/Asistente.Application/Asistente/PreguntarQuery.cs#L18) |
| 5 | `IAsistente` es `AsistenteIA` si hay clave, o `AsistenteNoConfigurado` (503) si no | [InfrastructureServiceExtensions.cs:60](../src/Asistente.Infrastructure/InfrastructureServiceExtensions.cs#L60) |
| 6 | `AsistenteIA` agrega el id del cliente al texto y crea las herramientas de esta pregunta | [AsistenteIA.cs:16](../src/Asistente.Infrastructure/IA/AsistenteIA.cs#L16), [Herramientas.cs:65](../src/Asistente.Infrastructure/IA/Herramientas.cs#L65) |
| 7 | `ConversacionIA` arma los mensajes (prompt de sistema y pregunta), fija el límite de 60 s y llama al modelo | [ConversacionIA.cs:17-33](../src/Asistente.Infrastructure/IA/ConversacionIA.cs#L17) |
| 8 | El cliente de IA es una **cadena de decoradores**: `FunctionInvokingChatClient` envuelve al cliente de Claude | [InfrastructureServiceExtensions.cs:87-91](../src/Asistente.Infrastructure/InfrastructureServiceExtensions.cs#L87) |
| 9 | Claude responde "quiero llamar a `BuscarProductos`". El decorador ejecuta la función (hasta 8 vueltas) | [Herramientas.cs:81](../src/Asistente.Infrastructure/IA/Herramientas.cs#L81) |
| 10 | La herramienta anota su uso y llama al puerto `ICatalogoConsultas` | [Herramientas.cs:135](../src/Asistente.Infrastructure/IA/Herramientas.cs#L135) |
| 11 | `CatalogoConsultasHttp.BuscarAsync` trae el catálogo por páginas de 100 y filtra sin tildes | [ConsultasHttp.cs:53](../src/Asistente.Infrastructure/Servicios/ConsultasHttp.cs#L53) |
| 12 | `BearerTokenHandler` pide el token. Recién ahora, por primera vez, se llama a Authentication | [BearerTokenHandler.cs:11](../src/Asistente.Infrastructure/Servicios/BearerTokenHandler.cs#L11), [TokenProvider.cs:55](../src/Asistente.Infrastructure/Servicios/TokenProvider.cs#L55) |
| 13 | Catálogo contesta; un 404 se traduce a "no existe" y cualquier otro fallo a 503 | [ConsultasHttp.cs:22-35](../src/Asistente.Infrastructure/Servicios/ConsultasHttp.cs#L22) |
| 14 | El resultado vuelve a Claude, que redacta la respuesta final | [ConversacionIA.cs:33](../src/Asistente.Infrastructure/IA/ConversacionIA.cs#L33) |
| 15 | `ConversacionIA` arma el `RespuestaDto` con las fuentes, las herramientas usadas y el consumo (`uso`), y lo escribe en el log | [ConversacionIA.cs:34-39](../src/Asistente.Infrastructure/IA/ConversacionIA.cs#L34) |

**Si algo falla en el camino:**
- Una herramienta que no logra consultar un servicio **corta la pregunta** y devuelve 503 ([ConversacionIA.cs:40](../src/Asistente.Infrastructure/IA/ConversacionIA.cs#L40), [Herramientas.cs:135](../src/Asistente.Infrastructure/IA/Herramientas.cs#L135)).
- Si el modelo tarda más de 60 s: 503 ([ConversacionIA.cs:45](../src/Asistente.Infrastructure/IA/ConversacionIA.cs#L45)).
- Si Anthropic falla: 503 ([ConversacionIA.cs:49](../src/Asistente.Infrastructure/IA/ConversacionIA.cs#L49)).

---

## 5. Recorrido 2: el apartado Sistemas

`POST /api/sistemas/preguntar` con *"¿Qué se rompe si cambio el DTO de productos de Catálogo?"*.

| # | Qué pasa | Dónde |
|---|---|---|
| 1 | El endpoint envía `PreguntarSobreSistemasQuery` (3 a 500 caracteres) | [SistemasEndpoints.cs:28](../src/Asistente.Api/Endpoints/SistemasEndpoints.cs#L28), [PreguntarSobreSistemasQuery.cs](../src/Asistente.Application/Sistemas/PreguntarSobreSistemasQuery.cs) |
| 2 | El handler llama a `ISistemasAsistente`, que es `SistemasIA` | [SistemasIA.cs:19](../src/Asistente.Infrastructure/IA/SistemasIA.cs#L19) |
| 3 | `SistemasIA` usa el **mismo** `ConversacionIA`, con otro prompt ([Instrucciones.cs:22](../src/Asistente.Infrastructure/IA/Instrucciones.cs#L22)) y hasta 2000 tokens de respuesta | [SistemasIA.cs:17](../src/Asistente.Infrastructure/IA/SistemasIA.cs#L17) |
| 4 | Sus 6 herramientas son `ListarSistemas`, `ObtenerSistema`, `ImpactoDeSistema`, `BuscarEnConocimiento`, `ListarDocumentos` y `LeerDocumento` | [HerramientasSistemas.cs:22](../src/Asistente.Infrastructure/IA/HerramientasSistemas.cs#L22) |
| 5 | Todas consultan a `ConocimientoEcosistema`. Si algo no existe, se lo cuentan al modelo con los nombres válidos, en vez de fallar | [HerramientasSistemas.cs:72](../src/Asistente.Infrastructure/IA/HerramientasSistemas.cs#L72) |
| 6 | `ConocimientoEcosistema` lee `sistemas.json` y los documentos, cargados al arrancar | [EstructuraSistemas.cs](../src/Asistente.Infrastructure/Mapa/EstructuraSistemas.cs), [BibliotecaDocumentos.cs](../src/Asistente.Infrastructure/Mapa/BibliotecaDocumentos.cs) |

**No llama a ningún otro sistema**: por eso funciona aunque Catálogo, Pedidos y Clientes estén apagados. Solo necesita la clave de Claude.

---

## 6. Recorrido 3: el healthcheck

`GET /api/asistente/salud` → [AsistenteEndpoints.cs:26](../src/Asistente.Api/Endpoints/AsistenteEndpoints.cs#L26) → `VerificarSaludQuery` → [VerificadorSalud.cs:25](../src/Asistente.Infrastructure/Servicios/VerificadorSalud.cs#L25).

| Componente | Cómo lo comprueba | Dónde |
|---|---|---|
| `Mapa` | Cuenta las herramientas cargadas | [VerificadorSalud.cs:31](../src/Asistente.Infrastructure/Servicios/VerificadorSalud.cs#L31) |
| `Authentication` | Pide un JWT nuevo, a propósito (`renovar: true`) | [VerificadorSalud.cs:53](../src/Asistente.Infrastructure/Servicios/VerificadorSalud.cs#L53) |
| `Catalogo` | `GET /api/productos/categorias` con token. Si Authentication falló, dice "Sin JWT" sin consultarlo | [VerificadorSalud.cs:42](../src/Asistente.Infrastructure/Servicios/VerificadorSalud.cs#L42) |
| `Pedidos` | `GET /api/pedidos?tamanio=1` | [VerificadorSalud.cs:34](../src/Asistente.Infrastructure/Servicios/VerificadorSalud.cs#L34) |
| `Clientes` | `GET /api/clientes?tamanio=1` | [VerificadorSalud.cs:35](../src/Asistente.Infrastructure/Servicios/VerificadorSalud.cs#L35) |
| `Modelo` | Lista los modelos con la clave (no genera texto) | [VerificadorSalud.cs:76](../src/Asistente.Infrastructure/Servicios/VerificadorSalud.cs#L76) |

Cada componente tiene 5 s de límite y todos corren en paralelo ([línea 23](../src/Asistente.Infrastructure/Servicios/VerificadorSalud.cs#L23)). Responde **200** si ninguno está en `Error` y **503** si alguno lo está. `SinConfigurar` (por ejemplo, sin clave) no cuenta como error.

---

## 7. Recorrido 4: el arranque

Qué pasa entre `dotnet run` y el primer pedido.

| # | Qué pasa | Dónde |
|---|---|---|
| 1 | `Program.cs` registra Application e Infrastructure | [Program.cs:12-17](../src/Asistente.Api/Program.cs#L12) |
| 2 | Se lee `mapa-sistema.json` y se **valida contra el código**: si no coinciden, el servicio **no arranca** | [InfrastructureServiceExtensions.cs:22](../src/Asistente.Infrastructure/InfrastructureServiceExtensions.cs#L22), [Herramientas.cs:52](../src/Asistente.Infrastructure/IA/Herramientas.cs#L52) |
| 3 | Se cargan `sistemas.json` y los documentos (si falta un archivo, no arranca) | [InfrastructureServiceExtensions.cs:25-27](../src/Asistente.Infrastructure/InfrastructureServiceExtensions.cs#L25) |
| 4 | Se registran los clientes HTTP, **sin conectarse a nadie** | [InfrastructureServiceExtensions.cs:34-37](../src/Asistente.Infrastructure/InfrastructureServiceExtensions.cs#L34) |
| 5 | Se registran `IAsistente` y `ISistemasAsistente` como fábricas: se deciden **al usarlas**, con la configuración ya completa | [InfrastructureServiceExtensions.cs:54-72](../src/Asistente.Infrastructure/InfrastructureServiceExtensions.cs#L54) |
| 6 | Al usarlas, `CrearModelo` mira `IA:Proveedor` e `IA:ApiKey`. Si no hay clave, deja el marcador "no configurado" y el servicio **arranca igual** | [InfrastructureServiceExtensions.cs:76-91](../src/Asistente.Infrastructure/InfrastructureServiceExtensions.cs#L76) |
| 7 | Las dos IAs comparten **un único** cliente de Claude | [InfrastructureServiceExtensions.cs:74](../src/Asistente.Infrastructure/InfrastructureServiceExtensions.cs#L74) |

Por eso el asistente arranca con los otros servicios apagados y sin clave: los problemas aparecen recién cuando alguien pregunta.

---

## 8. Las dos IAs y sus herramientas

| | IA de negocio | IA de Sistemas |
|---|---|---|
| Endpoint | `POST /api/asistente/preguntar` | `POST /api/sistemas/preguntar` |
| Clase | [AsistenteIA](../src/Asistente.Infrastructure/IA/AsistenteIA.cs) | [SistemasIA](../src/Asistente.Infrastructure/IA/SistemasIA.cs) |
| Prompt | [Instrucciones.Sistema](../src/Asistente.Infrastructure/IA/Instrucciones.cs#L6) | [Instrucciones.Ecosistema](../src/Asistente.Infrastructure/IA/Instrucciones.cs#L22) |
| Herramientas | 5, definidas en [mapa-sistema.json](../src/Asistente.Api/mapa-sistema.json) y [Herramientas.cs](../src/Asistente.Infrastructure/IA/Herramientas.cs) | 6, en [HerramientasSistemas.cs](../src/Asistente.Infrastructure/IA/HerramientasSistemas.cs) |
| De dónde saca datos | Las APIs de Catálogo, Pedidos y Clientes | `sistemas.json` y los documentos |
| Tokens de respuesta | 800 ([AsistenteIA.cs:14](../src/Asistente.Infrastructure/IA/AsistenteIA.cs#L14)) | 2000 |
| Motor común | [ConversacionIA](../src/Asistente.Infrastructure/IA/ConversacionIA.cs) | [ConversacionIA](../src/Asistente.Infrastructure/IA/ConversacionIA.cs) |

**Herramientas de negocio** (todas de solo lectura):

| Herramienta | Consulta | Línea |
|---|---|---|
| `ObtenerPrecioYStock` | Un producto por categoría e id (Catálogo) | [69](../src/Asistente.Infrastructure/IA/Herramientas.cs#L69) |
| `BuscarProductos` | Productos por nombre, sin tildes (Catálogo) | [81](../src/Asistente.Infrastructure/IA/Herramientas.cs#L81) |
| `UltimosPedidosDeCliente` | Pedidos recientes con sus líneas (Pedidos) | [92](../src/Asistente.Infrastructure/IA/Herramientas.cs#L92) |
| `EstadoDePedido` | Estado y total de un pedido (Pedidos) | [103](../src/Asistente.Infrastructure/IA/Herramientas.cs#L103) |
| `DatosDeCliente` | Nombre del cliente, sin email (Clientes) | [114](../src/Asistente.Infrastructure/IA/Herramientas.cs#L114) |

**Cómo se ve una herramienta por dentro:** una función con `[Description]` en cada parámetro, envuelta en `Ejecutar(...)`, que anota el uso y, si el servicio no responde, corta la pregunta. La descripción y el nombre salen del mapa, no del código.

**Regla de los prompts:** no nombran las herramientas, para que el modelo no las repita en sus respuestas; una prueba lo vigila ([SistemasIATests.cs:74](../tests/Asistente.Tests/SistemasIATests.cs#L74)).

---

## 9. Seguridad: dónde se aplica cada cosa

| Qué | Cómo | Dónde |
|---|---|---|
| Solo lectura | Ninguna herramienta escribe; además Catálogo responde 403 a las escrituras del asistente | [Herramientas.cs](../src/Asistente.Infrastructure/IA/Herramientas.cs), policies en [Catalogo.Api/Program.cs](../../CatalogoSystem/src/Catalogo.Api/Program.cs) |
| El email no sale | Los tipos que viajan al modelo no tienen ese campo | [Consultas.cs](../src/Asistente.Application/Asistente/Consultas.cs) |
| Identidad propia | Cliente de servicio `asistente-service` | [appsettings.json](../src/Asistente.Api/appsettings.json) (`Authentication:ClientId`) |
| Token | Se pide recién al primer uso y se renueva 1 minuto antes de vencer (dura 10 minutos) | [TokenProvider.cs:19](../src/Asistente.Infrastructure/Servicios/TokenProvider.cs#L19) |
| Token solo a Catálogo | Únicamente ese cliente HTTP lleva el `BearerTokenHandler` | [InfrastructureServiceExtensions.cs:35](../src/Asistente.Infrastructure/InfrastructureServiceExtensions.cs#L35) |
| Secretos | `IA:ApiKey` y `Authentication:ClientSecret` en User Secrets, fuera del repositorio | `Asistente.Api.csproj` (`UserSecretsId`) |
| Prompt injection | Prompt firme, solo herramientas de lectura y largo máximo de la pregunta | [Instrucciones.cs](../src/Asistente.Infrastructure/IA/Instrucciones.cs), [PreguntarQuery.cs:13](../src/Asistente.Application/Asistente/PreguntarQuery.cs#L13) |
| Su propia API | Abierta en desarrollo, sin autenticación | [Program.cs:54](../src/Asistente.Api/Program.cs#L54) |

**Pendiente de otros sistemas:** Pedidos y Clientes no exigen JWT en las llamadas entrantes; solo lo hace Catálogo.

---

## 10. Errores: de la excepción al código HTTP

Todo termina en [GlobalExceptionHandler.cs](../src/Asistente.Api/Infrastructure/GlobalExceptionHandler.cs#L17), que devuelve `application/problem+json`.

| Excepción | Código | Cuándo ocurre |
|---|---|---|
| `ValidationException` ([línea 19](../src/Asistente.Api/Infrastructure/GlobalExceptionHandler.cs#L19)) | **400** | Pregunta vacía, muy corta o muy larga; `clienteId` vacío |
| `BadHttpRequestException` ([30](../src/Asistente.Api/Infrastructure/GlobalExceptionHandler.cs#L30)) | **400** | JSON mal formado |
| `NotFoundException` ([36](../src/Asistente.Api/Infrastructure/GlobalExceptionHandler.cs#L36)) | **404** | Está mapeado, pero ningún endpoint lo produce hoy |
| `ServicioNoDisponibleException` o `HttpRequestException` ([42](../src/Asistente.Api/Infrastructure/GlobalExceptionHandler.cs#L42)) | **503** | Falta la clave, un servicio no responde, el modelo tarda más de 60 s o Anthropic falla |
| Cualquier otra ([48](../src/Asistente.Api/Infrastructure/GlobalExceptionHandler.cs#L48)) | **500** | Mensaje genérico; el detalle va al log ([línea 57](../src/Asistente.Api/Infrastructure/GlobalExceptionHandler.cs#L57)) |

---

## 11. Configuración, secretos y puertos

**[appsettings.json](../src/Asistente.Api/appsettings.json)** (sin secretos):

| Clave | Valor | Para qué |
|---|---|---|
| `IA:Proveedor` | `Anthropic` | Único proveedor implementado |
| `IA:ModeloChat` | `claude-haiku-5-5` | Modelo de Claude |
| `IA:MaxIteraciones` | `8` | Vueltas máximas de herramientas por pregunta |
| `Servicios:Authentication` / `Catalogo` / `Pedidos` / `Clientes` | `https://localhost:7401` / `7301` / `7101` / `7201` | A dónde llamar |
| `Authentication:ClientId` | `asistente-service` | Identidad del asistente |
| `Swagger:ClienteEjemplo` | un id de cliente | Ejemplo precargado en el Swagger |

**User Secrets** (no están en el repositorio):

| Clave | Para qué |
|---|---|
| `IA:ApiKey` | La clave de Claude (se crea en `platform.claude.com`) |
| `Authentication:ClientSecret` | El secreto de `asistente-service` (el mismo que `ClientCredentials:AsistenteSecret` en Authentication) |

**Tiempos límite:** 10 s por llamada a los servicios ([InfrastructureServiceExtensions.cs:15](../src/Asistente.Infrastructure/InfrastructureServiceExtensions.cs#L15)), 5 s por componente de salud y 60 s por pregunta.

**Puertos:** 7501 (https) y 5501 (http), en [launchSettings.json](../src/Asistente.Api/Properties/launchSettings.json). En VS Code, F5 usa [.vscode/launch.json](../.vscode/launch.json).

---

## 12. Patrones de diseño y dónde están

| Patrón | Qué resuelve | Dónde |
|---|---|---|
| **Clean Architecture** | Las dependencias van solo hacia adentro | Los 4 proyectos de `src/` |
| **Puertos y adaptadores** | Application define interfaces; Infrastructure las implementa | [IConsultas.cs](../src/Asistente.Application/Interfaces/IConsultas.cs) y [ConsultasHttp.cs](../src/Asistente.Infrastructure/Servicios/ConsultasHttp.cs) |
| **CQRS con MediatR** | Cada caso de uso es una consulta con su validador y su handler | [PreguntarQuery.cs](../src/Asistente.Application/Asistente/PreguntarQuery.cs) |
| **Decorador** | Agrega comportamiento sin cambiar el original | `FunctionInvokingChatClient` ([InfrastructureServiceExtensions.cs:90](../src/Asistente.Infrastructure/InfrastructureServiceExtensions.cs#L90)); los behaviors de MediatR; `BearerTokenHandler` sobre `HttpClient` |
| **Objeto nulo** | Un reemplazo seguro cuando falta algo | `AsistenteNoConfigurado` y `SistemasNoConfigurado` |
| **Fábrica perezosa** | Decide al usar, no al registrar | [InfrastructureServiceExtensions.cs:54-72](../src/Asistente.Infrastructure/InfrastructureServiceExtensions.cs#L54) |
| **Caché con margen** | Reutiliza el token hasta 1 minuto antes de que venza | [TokenProvider.cs:44](../src/Asistente.Infrastructure/Servicios/TokenProvider.cs#L44) |
| **Uso de herramientas (tool use)** | El modelo pide consultas y el código las ejecuta | [Herramientas.cs](../src/Asistente.Infrastructure/IA/Herramientas.cs) |

---

## 13. Pruebas

**79 pruebas con xUnit**, todas sin red y sin llamar a Claude.

| Proyecto | Pruebas | Cómo se evita Claude |
|---|---|---|
| [Asistente.Tests](../tests/Asistente.Tests) | 69 unitarias | Un `ModeloFalso` ([AsistenteIATests.cs:13](../tests/Asistente.Tests/AsistenteIATests.cs#L13)) con respuestas predefinidas, dentro del `FunctionInvokingChatClient` real |
| [Asistente.IntegrationTests](../tests/Asistente.IntegrationTests) | 10 de integración | `IA:ApiKey` forzada a vacío ([AsistenteApiTests.cs:11-14](../tests/Asistente.IntegrationTests/AsistenteApiTests.cs#L11)) |

| Archivo | Qué prueba |
|---|---|
| [AsistenteIATests.cs](../tests/Asistente.Tests/AsistenteIATests.cs) | La IA de negocio con un modelo falso: fuentes, herramientas usadas, consumo de tokens, servicio caído. Usa NSubstitute ([línea 44](../tests/Asistente.Tests/AsistenteIATests.cs#L44)) |
| [SistemasIATests.cs](../tests/Asistente.Tests/SistemasIATests.cs) | La IA de Sistemas, y que los prompts no nombren las herramientas ([74](../tests/Asistente.Tests/SistemasIATests.cs#L74)) |
| [HerramientasTests.cs](../tests/Asistente.Tests/HerramientasTests.cs) | El mapa contra el código ([`Validar`](../tests/Asistente.Tests/HerramientasTests.cs#L31)) y que solo se registren las de solo lectura |
| [ServiciosHttpTests.cs](../tests/Asistente.Tests/ServiciosHttpTests.cs) | Los adaptadores HTTP, el header `Authorization`, el cacheo del token y la búsqueda sin tildes. Define `ServidorFalso` ([11](../tests/Asistente.Tests/ServiciosHttpTests.cs#L11)), `TokenFalso` y `RelojFalso` |
| [SaludTests.cs](../tests/Asistente.Tests/SaludTests.cs) | El healthcheck: "Sin JWT" cuando Authentication falla |
| [SistemasTests.cs](../tests/Asistente.Tests/SistemasTests.cs) | `ConocimientoEcosistema`, los archivos reales (puertos contra `launchSettings.json`, [línea 195](../tests/Asistente.Tests/SistemasTests.cs#L195)) y que la base de conocimiento no mencione al asistente ([219](../tests/Asistente.Tests/SistemasTests.cs#L219)) |
| [ValidacionTests.cs](../tests/Asistente.Tests/ValidacionTests.cs) | Los validadores de entrada |
| [AsistenteApiTests.cs](../tests/Asistente.IntegrationTests/AsistenteApiTests.cs) | Los endpoints reales: 400, 503 y rutas que ya no existen |

**Correrlas:**

```powershell
cd AsistenteSystem
dotnet test
```

Si hay un Asistente corriendo (por ejemplo desde VS Code con F5), bloquea los DLL y la compilación falla: hay que detenerlo antes.

---

## 14. Datos, scripts y herramientas para probar

**Bases de datos** (carpeta `sql/` en la raíz del repositorio; ver su [README](../../sql/README.md)):

| Script | Qué crea |
|---|---|
| [01-PedidosDb-crear.sql](../../sql/01-PedidosDb-crear.sql) | Base y tablas de Pedidos |
| [02-PedidosDb-procedimientos.sql](../../sql/02-PedidosDb-procedimientos.sql) | Procedimientos almacenados |
| [03-CatalogoDb-crear.sql](../../sql/03-CatalogoDb-crear.sql) | Catálogo con **41 productos** en 6 categorías (2 sin stock) |
| [04-PedidosDb-datos-de-prueba.sql](../../sql/04-PedidosDb-datos-de-prueba.sql) | 100 clientes y 1.000 pedidos con 3 líneas, usando productos reales |
| [05-ClientesDb-crear.sql](../../sql/05-ClientesDb-crear.sql) | Base de Clientes |
| [06-AuthenticationDb-crear.sql](../../sql/06-AuthenticationDb-crear.sql) | Base de Authentication |

Todos crean **desde cero** y son idempotentes.

**Para probar a mano:**
- [Asistente.http](../Asistente.http): 18 peticiones (extensión REST Client de VS Code).
- Swagger: `https://localhost:7501/swagger`, con un cliente de ejemplo precargado en `preguntar`.

**Orden para levantar todo:** Authentication (7401) → Catálogo (7301) → Pedidos (7101) → Clientes (7201) → Asistente (7501). La guía paso a paso está en [instrucciones/06-levantar-y-probar-el-entorno.md](instrucciones/06-levantar-y-probar-el-entorno.md).

---

## 15. Paquetes y versiones

Todas se declaran en [Directory.Packages.props](../Directory.Packages.props).

| Paquete | Versión | Para qué |
|---|---|---|
| `Anthropic` | 12.60.0 | SDK oficial de Claude |
| `Microsoft.Extensions.AI` | 10.11.0 | `IChatClient` y los decoradores |
| `MediatR` | 12.5.0 | CQRS (la última con licencia Apache 2.0) |
| `FluentValidation` | 12.1.1 | Validadores |
| `Microsoft.Extensions.Http` | 10.0.13 | `IHttpClientFactory` |
| `Microsoft.AspNetCore.OpenApi` / `Swashbuckle.AspNetCore.SwaggerUI` | 10.0.12 / 10.2.3 | OpenAPI y Swagger |
| `xunit` / `xunit.runner.visualstudio` / `Microsoft.NET.Test.Sdk` | 2.9.3 / 3.1.0 / 17.13.0 | Pruebas |
| `NSubstitute` | 5.3.0 | Dobles de prueba |
| `Microsoft.AspNetCore.Mvc.Testing` | 10.0.12 | `WebApplicationFactory` |
| `SonarAnalyzer.CSharp` | 10.* | Análisis estático; `TreatWarningsAsErrors` está activo ([Directory.Build.props:7](../Directory.Build.props#L7)) |

Plataforma: **.NET 10**.

---

## 16. Si querés cambiar X, mirá Y

| Quiero... | Mirá |
|---|---|
| Cambiar cómo habla la IA de negocio o la de Sistemas | [Instrucciones.cs](../src/Asistente.Infrastructure/IA/Instrucciones.cs) |
| Agregar una herramienta de negocio | [mapa-sistema.json](../src/Asistente.Api/mapa-sistema.json) y [Herramientas.cs](../src/Asistente.Infrastructure/IA/Herramientas.cs) (las dos juntas, o no arranca) |
| Agregar una consulta a otro sistema | Un método en [IConsultas.cs](../src/Asistente.Application/Interfaces/IConsultas.cs) y su implementación en [ConsultasHttp.cs](../src/Asistente.Infrastructure/Servicios/ConsultasHttp.cs) |
| Cambiar el modelo de Claude | `IA:ModeloChat` en [appsettings.json](../src/Asistente.Api/appsettings.json) |
| Cambiar cuántas vueltas de herramientas hace | `IA:MaxIteraciones` |
| Cambiar el límite de 60 s por pregunta | [ConversacionIA.cs:15](../src/Asistente.Infrastructure/IA/ConversacionIA.cs#L15) |
| Cambiar el largo máximo de la respuesta | [AsistenteIA.cs:14](../src/Asistente.Infrastructure/IA/AsistenteIA.cs#L14) y [SistemasIA.cs:17](../src/Asistente.Infrastructure/IA/SistemasIA.cs#L17) |
| Cambiar el código HTTP de un error | [GlobalExceptionHandler.cs](../src/Asistente.Api/Infrastructure/GlobalExceptionHandler.cs#L17) |
| Agregar un decorador al cliente de IA (logging, caché) | La cadena de [InfrastructureServiceExtensions.cs:87-91](../src/Asistente.Infrastructure/InfrastructureServiceExtensions.cs#L87) |
| Agregar algo al healthcheck | [VerificadorSalud.cs:25](../src/Asistente.Infrastructure/Servicios/VerificadorSalud.cs#L25) |
| Que la IA de Sistemas conozca un cambio | [sistemas.json](../src/Asistente.Api/sistemas.json) y los documentos de [arquitectura](arquitectura) e [instrucciones](instrucciones) |
| Cambiar la validación de la pregunta | [PreguntarQuery.cs:13](../src/Asistente.Application/Asistente/PreguntarQuery.cs#L13) |
| Cambiar los puertos | [launchSettings.json](../src/Asistente.Api/Properties/launchSettings.json) y `Servicios:*` en [appsettings.json](../src/Asistente.Api/appsettings.json) |
| Cambiar la clave de Claude | `dotnet user-secrets set "IA:ApiKey" "..."` dentro de `src/Asistente.Api` |
| Cambiar el cliente de ejemplo de Swagger | `Swagger:ClienteEjemplo` en [appsettings.json](../src/Asistente.Api/appsettings.json) |

---

## 17. Límites y pendientes

**Lo que el asistente no hace:**
- No escribe: no crea, confirma ni cancela pedidos.
- No recuerda: cada pregunta es independiente, sin historial de conversación.
- No usa RAG ni embeddings: el catálogo (41 productos) se consulta entero.
- No tiene MCP.
- Su propia API no tiene autenticación.
- Solo implementa el proveedor `Anthropic`.

**Lo que depende de otros sistemas:**
- Pedidos y Clientes no exigen JWT en las llamadas entrantes.
- La IA de Sistemas solo sabe lo que está escrito en `sistemas.json` y los documentos: si cambia un sistema y no se actualizan, responde con información vieja.

**Lo que la especificación pedía y no se hizo:**
- Ollama y Azure OpenAI como proveedores.
- Un pipeline de IA con logging y caché distribuida (hoy solo ejecución de herramientas).
- Registrar el texto de cada pregunta y los argumentos de cada herramienta (hoy se registran los nombres de las herramientas, los tokens y la duración).
- El agente que propone pedidos (se quitó a propósito).

---

## 18. Ruta de lectura sugerida

Para entender el proyecto en unos 30 minutos, en este orden:

1. [README.md](../README.md): qué es y cómo correrlo (3 min).
2. [Program.cs](../src/Asistente.Api/Program.cs): cómo arranca (3 min).
3. [AsistenteEndpoints.cs](../src/Asistente.Api/Endpoints/AsistenteEndpoints.cs): la puerta de entrada (2 min).
4. [PreguntarQuery.cs](../src/Asistente.Application/Asistente/PreguntarQuery.cs) e [IConsultas.cs](../src/Asistente.Application/Interfaces/IConsultas.cs): casos de uso y puertos (4 min).
5. [ConversacionIA.cs](../src/Asistente.Infrastructure/IA/ConversacionIA.cs): el corazón (5 min).
6. [Herramientas.cs](../src/Asistente.Infrastructure/IA/Herramientas.cs) y [mapa-sistema.json](../src/Asistente.Api/mapa-sistema.json): las herramientas (5 min).
7. [InfrastructureServiceExtensions.cs](../src/Asistente.Infrastructure/InfrastructureServiceExtensions.cs): cómo se arma todo (4 min).
8. [TokenProvider.cs](../src/Asistente.Infrastructure/Servicios/TokenProvider.cs) y [ConsultasHttp.cs](../src/Asistente.Infrastructure/Servicios/ConsultasHttp.cs): hablar con otros sistemas (4 min).
9. [AsistenteIATests.cs](../tests/Asistente.Tests/AsistenteIATests.cs): cómo se prueba sin Claude (3 min).

Y para ver el producto funcionando: [Asistente.http](../Asistente.http).
