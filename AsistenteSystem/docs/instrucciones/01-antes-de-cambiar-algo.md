# Antes de cambiar algo

> Checklist de cinco minutos para no romper a otro sistema.

## 1. Entendé dónde está lo que querés cambiar

- Ubicá el sistema y la capa: cada sistema tiene sus proyectos Domain, Application, Infrastructure y Api (ver `capas-y-dependencias`).
- Revisá los endpoints, las capas y las tecnologías del sistema para encontrar el código que te interesa.

## 2. Averiguá quién lo usa

- Mirá qué sistemas consumen al tuyo y qué **contratos** usan: cada sistema indica de quién depende y quién lo consume.
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
- Actualizá la documentación si cambió algo que se prueba a mano.
- Commits pequeños, en español, uno por sistema, y no se hace push a `main` sin revisión.
