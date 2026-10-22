---
marp: true
theme: default
paginate: true
title: AsistenteSystem
description: Un asistente de IA que consulta el ecosistema de microservicios por sus APIs
---

<!--
Cómo usar este archivo
- Cada "---" separa una diapositiva.
- En VS Code, con la extensión "Marp for VS Code", se ve como presentación (botón de vista previa).
- Sin Marp, se lee igual como documento de Markdown.
-->

# AsistenteSystem

### Un asistente de IA para un ecosistema de microservicios

Certificación 2 · Máster Profesional en Microservicios con C# y .NET

**Qué vamos a ver:** qué hace, cómo está armado, cómo lo cuidamos y cuánto cuesta.

---

## El problema

Tenemos **cuatro microservicios**, cada uno con su base de datos:

| Sistema | Qué guarda |
|---|---|
| **Authentication** | Las credenciales de los sistemas |
| **Catálogo** | Productos, precios y stock |
| **Pedidos** | Pedidos y sus líneas |
| **Clientes** | Datos de los clientes |

Para contestar *"¿hay stock de yerba?"* o *"¿qué compró este cliente?"* hay que **saber a qué API ir, con qué ruta y con qué token**.

---

## La idea

> Que una persona **pregunte en lenguaje natural** y un modelo de IA decida **qué consultar**.

- El modelo **no inventa**: consulta los sistemas reales y responde con lo que encuentra.
- Los datos salen **solo de las APIs**, nunca de las bases.
- El asistente **solo lee**: no puede crear, cancelar ni modificar nada.

---

## Qué puede hacer: 4 endpoints

| Endpoint | Para qué |
|---|---|
| `POST /api/asistente/preguntar` | Responde con **datos reales** de productos, pedidos y clientes |
| `POST /api/sistemas/preguntar` | Explica **cómo está armado** el ecosistema (estructura, arquitectura, guías) |
| `GET /api/asistente/herramientas` | Lista lo que el asistente sabe consultar |
| `GET /api/asistente/salud` | Dice si todo lo que necesita está conectado |

Dos de ellos hablan con la IA; los otros dos son de consulta directa.

---

## El mapa de todo

```text
 Persona / Swagger / .http
          │
          ▼
   ┌──────────────────┐  JWT   ┌────────────────┐
   │   Asistente      │───────►│ Authentication │
   │ (Claude + tools) │        └────────────────┘
   └───┬─────┬─────┬──┘
       │     │     │   solo lectura, con Bearer
       ▼     ▼     ▼
   Catálogo Pedidos Clientes
       │     │       │
   CatalogoDb PedidosDb ClientesDb      ← el asistente nunca entra a las bases
```

Y hacia afuera, **una sola llamada externa**: la API de Claude (Anthropic).

---

## Demo 1: una pregunta de negocio

**Pregunta:** *¿Hay stock de yerba?*

> Sí, hay stock de yerba. Encontré un producto:
> **Yerba mate 1 kg** (Abarrotes): precio $4,20, stock disponible: 12 unidades.

- Herramienta usada: buscar productos
- Fuente: Catálogo
- Tiempo: ~2,4 segundos

El asistente **eligió solo** qué consultar y **cómo**.

---

## Demo 2: casos que importan

| Pregunta | Qué responde |
|---|---|
| ¿Hay café molido? | Existe, cuesta $5,90 y está **agotado** (stock 0) |
| ¿Tienen pizza congelada? | **No lo encontró** y lo dice, sin inventar |
| ¿Qué compré en mis últimos pedidos? | La lista de productos de los últimos 5 pedidos del cliente |
| ¿Cómo me llamo? | El nombre del cliente (**nunca su email**) |
| Creá un pedido de 10 yerbas | *"No puedo crear pedidos... hacelo en el sistema de Pedidos"* |

---

## Cómo piensa: tool use

```text
 1. Pregunta ──► 2. Claude decide qué herramienta usar
                         │
                         ▼
                 3. El asistente la ejecuta
                    (llama a la API real)
                         │
                         ▼
                 4. Claude lee el resultado
                    y puede pedir otra herramienta
                         │
                         ▼
                 5. Respuesta final, con fuentes
```

Hasta **8 vueltas** por pregunta. Si un servicio falla, la pregunta se corta con un 503.

---

## Las herramientas de negocio

| Herramienta | Consulta |
|---|---|
| `ObtenerPrecioYStock` | Un producto por categoría e id (Catálogo) |
| `BuscarProductos` | Productos por nombre, **sin importar tildes** (Catálogo) |
| `UltimosPedidosDeCliente` | Pedidos recientes con sus líneas (Pedidos) |
| `EstadoDePedido` | Estado y total de un pedido (Pedidos) |
| `DatosDeCliente` | Nombre del cliente (Clientes) |

Salen de un archivo, `mapa-sistema.json`: **si el código y el mapa no coinciden, el servicio no arranca**.

---

## El apartado Sistemas

**Un solo endpoint** para preguntar por el ecosistema:

> *¿Qué se rompe si cambio el DTO de productos de Catálogo?*

Responde con los sistemas y contratos afectados, los puntos de atención y **cita el sistema o documento** de donde sale cada dato.

También contesta: *¿cómo agrego un endpoint en Pedidos?*, *¿cómo se manejan los errores?*, *¿qué sistemas hay y cómo se hablan?*

**No necesita** que los otros servicios estén levantados.

---

## De dónde sabe lo que sabe

Tres fuentes, todas **archivos del repositorio**:

| Fuente | Contenido |
|---|---|
| `sistemas.json` | Estructura de los 4 sistemas: puertos, capas, endpoints, dependencias, contratos |
| `docs/arquitectura/` | 7 documentos: capas, CQRS, errores, seguridad, datos, pruebas |
| `docs/instrucciones/` | 5 guías paso a paso: agregar un endpoint, cambiar un contrato... |

Si cambia un sistema, **hay que actualizar esos archivos**: la IA solo sabe lo que está escrito.

---

## Las reglas que no se negocian

1. **Sin base propia.** Todo dato sale de las APIs de los otros servicios.
2. **Solo lectura.** Ninguna herramienta modifica datos.
3. **No duplica reglas de negocio.** Si la regla vive en Pedidos, se le pregunta a Pedidos.
4. **Identidad propia.** Tiene su cliente de servicio y su JWT.
5. **La IA es un detalle de Infrastructure.** El resto del código no la conoce.
6. **Secretos fuera del código.** La clave de Claude vive en User Secrets.
7. **Todo queda registrado.** Herramientas usadas, tokens y duración de cada pregunta.

---

## Seguridad

| Riesgo | Cómo se cuida |
|---|---|
| El asistente modifica datos | No tiene **ninguna** herramienta de escritura; en Catálogo recibe 403 si lo intenta |
| Se filtra el email del cliente | Los tipos que viajan al modelo **ni siquiera tienen** ese campo |
| *"Ignorá tus instrucciones..."* | Prompt firme + solo herramientas de lectura. Probado: **se negó** |
| Se pide la clave o los secretos | Se negó y explicó qué sí puede hacer |
| La clave de Claude en Git | Está en User Secrets; se verificó que **no está en el historial** |

---

## Identidad: el token se pide solo si hace falta

- Al arrancar, el asistente **no se conecta a nadie**.
- La primera vez que una herramienta necesita Catálogo, pide un **JWT** a Authentication.
- Lo guarda en memoria hasta **1 minuto antes de que venza** (dura 10 minutos).
- Catálogo tiene dos permisos: **lectura** (para el asistente) y **escritura** (solo Pedidos).

Resultado: el asistente **arranca y responde** aunque los demás servicios estén apagados.

---

## Cuando algo falla

| Situación | Qué pasa |
|---|---|
| Catálogo caído | **503** con el motivo real; Claude no inventa una explicación |
| Falta la clave de Claude | **503** claro: "el modelo no está configurado" |
| Pregunta vacía o muy corta | **400** con el detalle de validación |
| JSON mal formado | **400** (antes era un 500) |
| El modelo tarda más de 60 s | **503** |

Una herramienta que falla **corta la pregunta**: nunca se responde con datos a medias.

---

## Healthcheck: ¿está todo conectado?

`GET /api/asistente/salud` revisa seis cosas y devuelve **200** o **503**:

| Componente | Qué comprueba |
|---|---|
| Mapa | Las herramientas cargaron |
| Authentication | Que entrega un JWT |
| Catálogo | Responde con ese token |
| Pedidos | Responde |
| Clientes | Responde |
| Modelo | La clave de Claude es válida (sin gastar tokens) |

Las **bases se verifican a través de cada API**: el asistente nunca las toca.

---

## Transparencia: cada respuesta se explica sola

```json
{
  "respuesta": "Sí, hay stock de yerba...",
  "fuentes": [ { "servicio": "Catalogo", "referencia": "Abarrotes/..." } ],
  "herramientasUsadas": [ "BuscarProductos" ],
  "uso": { "modelo": "claude-haiku-5-5",
           "tokensEntrada": 3091, "tokensSalida": 144, "duracionMs": 2383 }
}
```

Se sabe **de dónde salió** el dato, **qué se consultó** y **cuánto costó**.

---

## ¿Cuánto cuesta?

Modelo `claude-haiku-5-5`: **0,10 US$** por millón de tokens de entrada y **0,50 US$** de salida.

| Tipo de pregunta | Tokens (entrada / salida) | Costo aprox. |
|---|---|---|
| De negocio: *¿hay stock de yerba?* | ~3.100 / ~150 | **0,0004 US$** |
| De sistemas: *¿qué se rompe si cambio X?* | 7.000 a 16.000 / 600 a 1.800 | **0,002 a 0,003 US$** |

Decenas de pruebas reales durante el desarrollo: **unos pocos centavos**. El modelo se cambia en la configuración.

---

## Por dentro: Clean Architecture

```text
Asistente.Api             Endpoints finos, manejo de errores (ProblemDetails)
      │
Asistente.Application     Casos de uso (MediatR), validaciones, PUERTOS
      │                    ICatalogoConsultas, IPedidosConsultas, IAsistente...
Asistente.Domain          Tipos simples
      ▲
Asistente.Infrastructure  La IA, los clientes HTTP, el token, el healthcheck
```

**Application define las interfaces; Infrastructure las implementa.** Ninguna capa interna conoce a Claude.

---

## Tecnología

| Capa | Qué se usa |
|---|---|
| Plataforma | .NET 10, ASP.NET Core, Minimal APIs |
| Patrones | CQRS con MediatR 12.5.0, FluentValidation 12.1.1 |
| IA | SDK de **Anthropic** + `Microsoft.Extensions.AI` (`IChatClient`) |
| Comunicación | `HttpClient` por `IHttpClientFactory`, JWT Bearer |
| Calidad | SonarAnalyzer con `TreatWarningsAsErrors` |
| Documentación | Swagger UI + archivo `.http` con 18 peticiones |

**40 archivos** de código y unas **1.600 líneas**.

---

## Pruebas: 79 y ninguna toca a Claude

| Tipo | Cantidad | Cómo |
|---|---|---|
| Unitarias | **69** | Un **modelo falso** que responde lo que se le indica |
| Integración | **10** | El servidor real con `WebApplicationFactory` |

- El modelo falso permite probar **qué herramienta se llama y con qué argumentos**.
- La clave se **fuerza vacía** en los tests: nunca gastan dinero.
- Una prueba vigila que los **puertos** del archivo de estructura coincidan con los reales.

---

## Decisiones de diseño

| Decisión | Por qué |
|---|---|
| **Claude** y no un modelo local | No hay que instalar ni mantener nada local; costo de centavos |
| **REST** y no gRPC hacia Catálogo | Más simple; gRPC queda como mejora |
| **Sin RAG ni embeddings** | El catálogo (41 productos) se consulta entero: no hace falta buscar por similitud |
| **Un solo endpoint** de Sistemas | La IA decide qué mirar; no hay que mantener ocho rutas |
| Herramientas **de solo lectura** | Reduce el daño posible a cero |
| Estructura en **archivos** (no en base) | Se versiona y se revisa como código |

---

## Lo que aprendimos (errores reales)

1. **La librería esconde los errores de una herramienta** y se los devuelve al modelo: con Catálogo caído habría respondido "hubo un error" con 200. Ahora corta con 503.
2. **Un test llamaba a Claude de verdad** con la clave del equipo. Se arregló de raíz.
3. **El modelo repetía los nombres de las herramientas** porque los leía en su prompt. Se quitaron.
4. **La búsqueda no encontraba "café"** (el catálogo dice "Cafe"). Ahora ignora tildes.
5. **Un secreto con salto de línea** daba 401: el hash no coincidía.

---

## Límites: lo que NO hace

- **No escribe**: no crea, confirma ni cancela pedidos.
- **No recuerda**: cada pregunta es independiente, sin historial.
- **No es determinista**: un modelo de lenguaje puede variar la redacción.
- **No se entera solo de los cambios**: depende de que se actualicen los archivos.
- **Su propia API está abierta** en desarrollo; para producción faltaría protegerla.
- **Depende de Claude**: sin internet o sin clave, responde 503.

---

## Los datos de la demo

| Dato | Valor |
|---|---|
| Productos | **41**, en 6 categorías (Abarrotes, Bebidas, Conservas, Galletas, Lacteos, Limpieza) |
| Con stock 0 | 2 (café molido y gaseosa lima limón) |
| Clientes | 100 (los crea el script) |
| Pedidos | 1.000, con 3 líneas cada uno |
| Cliente de ejemplo | **Cliente 75**: 10 pedidos y 5 productos favoritos (su id cambia si se recrea la base: `Swagger:ClienteEjemplo`) |

Todo se crea **desde cero** con los scripts de la carpeta `sql/`.

---

## Cómo correrlo

1. Crear las bases con los scripts de `sql/` (del 01 al 06).
2. Guardar los secretos: `Jwt:SigningKey`, los secretos de los clientes de servicio y la clave de Claude (`IA:ApiKey`).
3. Levantar **en orden**: Authentication → Catálogo → Pedidos → Clientes → Asistente.
4. Abrir `https://localhost:7501/swagger`.
5. Comprobar `GET /api/asistente/salud` → los seis componentes en **Ok**.
6. Probar con `Asistente.http` o con el Swagger.

---

## Guion de la demo (10 minutos)

| Min | Qué mostrar |
|---|---|
| 0 | `salud`: todo conectado |
| 1 | *¿Hay stock de yerba?* → respuesta con fuente y consumo |
| 3 | *¿Hay café molido?* → distingue "existe" de "hay stock" |
| 4 | *¿Qué compré en mis últimos pedidos?* con el Cliente 75 |
| 6 | *Creá un pedido* → se niega: solo lectura |
| 7 | **Apagar Catálogo** y repetir → 503 claro, sin inventar |
| 8 | Apartado Sistemas: *¿qué se rompe si cambio el DTO de productos?* |
| 9 | Mostrar `uso`: cuánto costó todo |

---

## En resumen

- Un asistente que **responde con datos reales**, no con suposiciones.
- **Solo lee**, no expone datos personales y se niega a salirse de sus reglas.
- **Arquitectura limpia**: la IA es un detalle intercambiable.
- **Transparente**: dice de dónde sacó cada dato y cuánto costó.
- **Probado**: 79 pruebas y verificado con Claude real.

**Próximos pasos posibles:** memoria de conversación, protección de su propia API, y proteger Pedidos y Clientes con JWT.

---

# Preguntas

`AsistenteSystem` · .NET 10 · Claude · Certificación 2
