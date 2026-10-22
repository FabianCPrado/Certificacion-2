# Cambiar un contrato entre sistemas

> Un contrato es todo lo que un sistema expone y otro consume: una ruta, un DTO, un campo, un código de error. Cambiarlo mal rompe al consumidor sin avisar.

## Paso a paso

1. **Averiguá quién lo consume:** `GET /api/sistemas/{nombre}/impacto`. Ahí aparecen los sistemas y el contrato exacto.
2. **Preferí agregar a modificar.** Agregar un campo opcional o una ruta nueva no rompe a nadie; renombrar o quitar algo sí.
3. **Si tenés que romper la compatibilidad:**
   - Primero agregá lo nuevo manteniendo lo viejo.
   - Cambiá a los consumidores uno por uno.
   - Recién después quitá lo viejo.
4. **Actualizá a los consumidores en el mismo cambio** cuando son pocos:
   - Adaptadores HTTP (por ejemplo `CatalogoConsultasHttp`, `PedidosConsultasHttp`).
   - Las interfaces y los fakes de sus tests.
5. **Verificá con datos reales:** levantá los sistemas involucrados y repetí la llamada. Un cambio de esquema que solo se prueba con fakes puede fallar con la base real.
6. **Dejá todo al día:** `sistemas.json` (endpoints y contratos), `docs/base-conocimiento` y `Asistente.http`.

## Ejemplos reales de este proyecto

| Cambio | Quién lo notó |
|---|---|
| Agregar `Categoria` a `LineaPedidoDto` | El detalle de pedidos falló hasta migrar la columna `Categoria` de la base de Pedidos |
| Separar la policy de Catalogo en lectura y escritura | El asistente pasó de 403 a 200 para leer, y sigue en 403 para escribir |
| El email viaja en `PedidoDto` y `ClienteDto` | El asistente tuvo que mapear a tipos propios sin ese campo |

## Qué contratos tiene hoy el proyecto

Mirá la sección `contratos` de cada sistema en `GET /api/sistemas/{nombre}`.
