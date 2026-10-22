# Antes de cambiar algo

> Checklist de cinco minutos para no romper a otro sistema.

## 1. Entendé dónde está lo que querés cambiar

- Preguntale a la IA en `POST /api/sistemas/preguntar`: "¿Dónde está lo de stock?", "¿Qué endpoints tiene Pedidos?". Busca por vos en la estructura, la arquitectura y las instrucciones.
- Preguntale por un sistema: "¿Cómo está armado Catálogo?" te explica sus capas, endpoints y tecnologías.

## 2. Averiguá quién lo usa

- Preguntale el impacto: "¿Qué se rompe si cambio X?" te dice qué sistemas consumen al tuyo y qué **contratos** usan.
- Si lo que cambiás es un contrato (un DTO, una ruta, un campo), seguí `cambiar-un-contrato`.

## 3. Elegí la capa correcta

| Si querés... | Va en... |
|---|---|
| Una regla de negocio | La entidad de Domain |
| Un caso de uso nuevo | Application (command o query con su validador) |
| Hablar con una base o con otro sistema | Infrastructure, detrás de una interfaz de Application |
| Una ruta nueva | Api, delgada, que solo manda a MediatR |

## 4. Hacelo pequeño y con test

- Un cambio por vez, con su test.
- `dotnet build` y `dotnet test` sin errores ni avisos en el sistema que tocaste y en los que lo consumen.

## 5. Dejá todo al día

- Actualizá `sistemas.json` si cambiaste endpoints, puertos o dependencias.
- Actualizá `Asistente.http` o la documentación si cambió algo que se prueba a mano.
- Commits pequeños, en español, uno por sistema, y no se hace push a `main` sin revisión.
