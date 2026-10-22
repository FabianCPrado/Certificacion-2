# Cambiar un contrato entre sistemas

> Un contrato es todo lo que un sistema expone y otro consume: una ruta, un DTO, un campo, un código de error. Cambiarlo mal rompe al consumidor sin avisar.

## Paso a paso

1. **Averiguá quién lo consume:** cada sistema lista sus contratos y quién los usa. Buscá el contrato que vas a tocar y anotá los sistemas que aparecen.
2. **Preferí agregar a modificar.** Agregar un campo opcional o una ruta nueva no rompe a nadie; renombrar o quitar algo sí.
3. **Si tenés que romper la compatibilidad:**
   - Primero agregá lo nuevo manteniendo lo viejo.
   - Cambiá a los consumidores uno por uno.
   - Recién después quitá lo viejo.
4. **Actualizá a los consumidores en el mismo cambio** cuando son pocos:
   - Los adaptadores HTTP que llaman a ese sistema (por ejemplo `CatalogoRestProductos` o `ClientesRestConsultas` en Pedidos).
   - Las interfaces y los fakes de sus tests.
5. **Verificá con datos reales:** levantá los sistemas involucrados y repetí la llamada. Un cambio de esquema que solo se prueba con fakes puede fallar con la base real.
6. **Dejá todo al día:** `sistemas.json` (endpoints y contratos) y la documentación del sistema.

## Ejemplos reales de este proyecto

| Cambio | Qué pasó |
|---|---|
| Agregar `Categoria` a `LineaPedidoDto` | El detalle de pedidos falló hasta migrar la columna `Categoria` de la base de Pedidos |
| Separar la policy de Catalogo en lectura y escritura | Los servicios con permiso de lectura pasaron a leer con 200 y siguen recibiendo 403 al escribir |
| El email viaja en `PedidoDto` y `ClienteDto` | Quien consume esos datos debe descartar el email si no lo necesita |

## Qué contratos tiene hoy el proyecto

Están en la sección `contratos` de cada sistema en `sistemas.json`.
