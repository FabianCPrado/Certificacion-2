# Base de conocimiento · AsistenteSystem

Fuente de verdad **local** sobre las APIs que consulta el asistente. Cada dato sale del código real de los otros servicios (no de la especificación), y cada archivo indica de dónde se tomó.

| Archivo | Contenido |
|---|---|
| [authentication.md](authentication.md) | Cómo se obtiene el JWT (`POST /api/tokens`) |
| [catalogo.md](catalogo.md) | Productos: precio, stock y búsqueda |
| [pedidos.md](pedidos.md) | Pedidos de un cliente y su estado |
| [clientes.md](clientes.md) | Datos básicos del cliente |
| [verificacion-mapa.md](verificacion-mapa.md) | `mapa-sistema.json` contrastado con las APIs reales: qué coincide y qué hay que corregir |

## Puertos locales

| Servicio | https | http |
|---|---|---|
| Pedidos | 7101 | 5101 |
| Clientes | 7201 | 5201 |
| Catálogo | 7301 | — |
| Authentication | 7401 | — |
| **Asistente** | **7501** | **5501** |

## Cómo usarla en clase

1. Empezar por `verificacion-mapa.md`: muestra por qué el asistente **no debe confiar en la especificación** sin contrastarla con el código.
2. Seguir el flujo de una pregunta ("¿hay stock de yerba?") por `authentication.md` → `catalogo.md`.
3. Mostrar que `GET /api/asistente/herramientas` devuelve el mismo mapa que aquí se documenta.

## Alcance

Solo se documentan los endpoints de **lectura** que usa el asistente. Los endpoints de escritura de cada servicio se listan únicamente para dejar claro que el asistente **no** los usa (regla 2: solo lectura).

Verificado el 2026-10-22 sobre la rama `feature/asistente`.
