# Seguridad

> Los sistemas se autentican entre sí con JWT emitidos por Authentication. Los secretos viven en User Secrets, nunca en Git.

## Clientes de servicio y JWT

- Authentication guarda **clientes de servicio** (por ejemplo `pedidos-service`) con su secreto hasheado (PBKDF2 de ASP.NET Identity).
- `POST /api/tokens` con `clientId` y `clientSecret` devuelve un JWT firmado (`accessToken`, `tokenType`, `expiresAt`).
- El JWT dura **10 minutos**, lleva `client_id` y `client_type = service`; el emisor es `AuthenticationSystem` y la audiencia `catalogo-api`.
- Catalogo valida la firma con la misma clave (`Jwt:SigningKey`).
- Los clientes de servicio los crea Authentication al arrancar en Development, a partir de User Secrets.
- Una identidad desactivada no obtiene tokens.

## Qué exige cada sistema

| Sistema | Exige JWT a quien lo llama |
|---|---|
| Catalogo | Sí. `LecturaCatalogo` (servicios autorizados solo para leer) para leer; `PedidosService` para escribir |
| Pedidos | No |
| Clientes | No |
| Authentication | No (es quien emite) |

## Secretos (User Secrets)

| Secreto | Dónde |
|---|---|
| `Jwt:SigningKey` | Authentication y Catalogo (el mismo valor) |
| `ClientCredentials:PedidosSecret` | Authentication y Pedidos |

Nunca van en `appsettings.json` ni en Git. Al guardar un secreto desde la terminal, cuidado con caracteres de fin de línea (`\r`): un secreto con `\r` hace que el hash no coincida y da 401.

## Reglas

1. **Mínimo permiso:** cada cliente de servicio solo puede lo que necesita. Un servicio con permiso de lectura recibe 403 si intenta escribir.
2. **Datos personales:** el email y la dirección de un cliente son datos sensibles; solo deben viajar entre los sistemas que los necesitan.
3. **Secretos fuera del código:** en User Secrets o en el gestor de secretos del entorno, nunca en el repositorio.
