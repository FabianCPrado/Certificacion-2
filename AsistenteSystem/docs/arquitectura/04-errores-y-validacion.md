# Errores y validación

> Un único lugar (`GlobalExceptionHandler`) convierte las excepciones en respuestas `ProblemDetails`. Los handlers lanzan excepciones, no códigos HTTP.

## Códigos que usa el sistema

| Código | Cuándo | Cómo se produce |
|---|---|---|
| 400 | Datos inválidos o JSON mal formado | `ValidationException` (FluentValidation) o `BadHttpRequestException` |
| 401 | Sin token o token inválido | Autenticación JWT (Catalogo) |
| 403 | Token válido sin permiso para ese endpoint | Policy de autorización (Catalogo) |
| 404 | No existe lo pedido | `NotFoundException` |
| 409 | Conflicto: email duplicado, stock insuficiente, regla de negocio | `ReglaNegocioException` u otra de conflicto |
| 503 | Un servicio del que dependemos no responde | `ServicioNoDisponibleException` o `HttpRequestException` |
| 500 | Error no previsto (se registra el detalle en el log, no se devuelve) | cualquier otra |

## Reglas

1. **El handler lanza excepciones; el `GlobalExceptionHandler` decide el código.** No se arma `StatusCode` en el controlador.
2. **Las respuestas de error son siempre `ProblemDetails`** (`application/problem+json`), con `title`, `status`, `detail` y `traceId`.
3. **Un 500 nunca expone el mensaje interno.** Responde un texto genérico y deja el detalle en el log.
4. **Si otro servicio falla, se responde 503, no 200.** Ejemplo: Clientes no elimina un cliente si Pedidos no responde (falla cerrado).
5. **Un servicio que falla no se traga el error.** Quien lo llamó responde 503 en vez de seguir con datos incompletos o inventados.

## Validación

- Se declara en un `Validator` junto al caso de uso, con FluentValidation.
- Ejemplos reales: `Pregunta` entre 3 y 500 caracteres; `tamanio` entre 1 y 100; `ClienteId` no puede ser `Guid.Empty`.
- Si la regla es del negocio (no del formato), va en la entidad de Domain y lanza `ReglaNegocioException`.
