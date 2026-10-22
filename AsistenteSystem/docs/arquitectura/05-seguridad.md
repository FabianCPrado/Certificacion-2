# Seguridad

> Los sistemas se autentican entre sí con JWT emitidos por Authentication. Los secretos viven en User Secrets, nunca en Git.

## Clientes de servicio y JWT

- Authentication guarda **clientes de servicio** (`pedidos-service`, `asistente-service`) con su secreto hasheado (PBKDF2 de ASP.NET Identity).
- `POST /api/tokens` con `clientId` y `clientSecret` devuelve un JWT firmado (`accessToken`, `tokenType`, `expiresAt`).
- El JWT lleva `client_id` y `client_type = service`; el emisor es `AuthenticationSystem` y la audiencia `catalogo-api`.
- Catalogo valida la firma con la misma clave (`Jwt:SigningKey`).
- Los clientes de servicio los crea Authentication al arrancar en Development, a partir de User Secrets.

## Qué exige cada sistema

| Sistema | Exige JWT a quien lo llama |
|---|---|
| Catalogo | Sí. `LecturaCatalogo` (pedidos-service y asistente-service) para leer; `PedidosService` solo para escribir |
| Pedidos | No |
| Clientes | No |
| Authentication | No (es quien emite) |
| Asistente | No (abierto en desarrollo) |

## Secretos (User Secrets)

| Secreto | Dónde |
|---|---|
| `Jwt:SigningKey` | Authentication y Catalogo (el mismo valor) |
| `ClientCredentials:PedidosSecret` | Authentication y Pedidos |
| `ClientCredentials:AsistenteSecret` | Authentication |
| `Authentication:ClientSecret` | Asistente (el mismo valor que `AsistenteSecret`) |
| `IA:ApiKey` | Asistente (clave de Anthropic) |

Nunca van en `appsettings.json` ni en Git. Al guardar un secreto desde la terminal, cuidado con caracteres de fin de línea (`\r`): un secreto con `\r` hace que el hash no coincida y da 401.

## Reglas

1. **El asistente es de solo lectura.** Ninguna herramienta modifica datos; lo que implique un cambio lo hace una persona en Pedidos.
2. **Datos personales:** el email del cliente nunca llega al modelo. Los adaptadores devuelven tipos propios sin ese campo.
3. **Mínimo permiso:** cada cliente de servicio solo puede lo que necesita (el asistente lee Catalogo, no escribe).
4. **Prompt injection:** instrucciones de sistema firmes, solo herramientas de lectura, preguntas con largo máximo y respuestas que nunca ejecutan nada.
