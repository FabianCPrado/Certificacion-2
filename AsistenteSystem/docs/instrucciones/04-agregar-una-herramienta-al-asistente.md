# Agregar una herramienta al asistente

> Una herramienta es una consulta que Claude puede pedir. Siempre es de solo lectura y sale del mapa del sistema.

## Pasos

1. **Confirmá que el dato existe** en la API del otro sistema y que `asistente-service` puede leerlo (en Catalogo, con la policy `LecturaCatalogo`).
2. **Agregá la fila a `mapa-sistema.json`:** `nombre`, `servicio`, `endpoint`, `descripcion` y `soloLectura: true`. La descripción es lo que Claude lee para decidir cuándo usarla: que sea clara.
3. **Application:** agregá el método a la interfaz de consultas (`ICatalogoConsultas`, `IPedidosConsultas` o `IClientesConsultas`) y, si hace falta, un tipo nuevo en `Consultas.cs`. **Sin email ni datos sensibles.**
4. **Infrastructure:**
   - Implementalo en `ConsultasHttp.cs`: un 404 devuelve `null`, cualquier otro fallo lanza `ServicioNoDisponibleException`.
   - Agregá la función en `Herramientas.cs`, con `[Description]` en cada parámetro, envuelta en `Ejecutar(...)` para registrar el uso y cortar la pregunta si el servicio falla.
   - Sumá el nombre a la lista `Implementadas`.
5. **El servicio no arranca si el mapa y el código no coinciden:** una herramienta en el mapa sin código (o al revés) falla al iniciar. Es a propósito.
6. **Tests:**
   - `HerramientasTests`: la nueva aparece solo si es de solo lectura.
   - `ServiciosHttpTests`: la ruta, el 404 y el error del servicio.
   - `AsistenteIATests`: con un modelo falso que llama a la herramienta.
7. **Probala con Claude real** (cuesta centavos) con una pregunta que la necesite y revisá `herramientasUsadas` en la respuesta.
8. **Dejá todo al día:** `sistemas.json` y `docs/base-conocimiento`.

## Reglas que no se negocian

- Ninguna herramienta modifica datos.
- Ninguna herramienta devuelve email ni medios de pago.
- Si un servicio no responde, la pregunta termina en 503; el modelo no improvisa.
