# Authentication · :7401

Fuente: `AuthenticationSystem/src/Authentication.Api/Endpoints/TokenEndpoints.cs` y `AuthenticationDbInitializer.cs`.

## Emitir un token

`POST /api/tokens`

```json
{ "clientId": "asistente-service", "clientSecret": "<secreto de 32 a 256 caracteres>" }
```

| Respuesta | Cuerpo |
|---|---|
| `200` | `{ "accessToken": "...", "tokenType": "...", "expiresAt": "2026-10-22T12:00:00+00:00" }` |
| `400` | ProblemDetails (cuerpo inválido) |
| `401` | credenciales incorrectas |

El asistente cachea el token hasta 1 minuto antes de `expiresAt` y lo manda como `Authorization: Bearer <token>`.

## Clientes de servicio dados de alta

| ClientId | Estado |
|---|---|
| `pedidos-service` | Se crea al iniciar, solo si existe `ClientCredentials:PedidosSecret` en User Secrets |
| `asistente-service` | **No existe todavía.** Es parte de la tarea 0 |

El inicializador hashea el secreto y lo guarda; nunca se almacena en claro.

## Claims que importan

Catálogo exige `client_id` y `client_type = service` (ver [catalogo.md](catalogo.md)).
