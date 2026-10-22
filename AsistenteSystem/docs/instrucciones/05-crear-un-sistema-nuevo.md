# Crear un sistema nuevo

> Un sistema nuevo es una carpeta hermana de las demás, con la misma estructura de cuatro capas.

## Antes de empezar, definí

1. **Nombre:** por ejemplo `Proveedores` genera la carpeta `ProveedoresSystem` y los namespaces `Proveedores.*`.
2. **Base de datos:** una `[Nombre]Db` nueva en SQL Server LocalDB, salvo que haya una razón para otra cosa.
3. **Ruta base:** por ejemplo `api/proveedores`.
4. **Puertos libres** (https / http). Hoy están ocupados del 7101 al 7501 de a cien (y sus http 5101 a 5501): usá el siguiente libre.

## Pasos

1. Ejecutá el comando `/crear-api`. Te pregunta lo anterior y genera Domain, Application, Infrastructure, Api y los tests.
2. **Revisá que compile sin avisos:** `dotnet build` y `dotnet test`.
3. **Script SQL:** agregalo en `sql/` con el mismo estilo (crea la base desde cero y es idempotente) y sumalo al README de `sql/` respetando el orden.
4. **Seguridad:** decidí si exige JWT. Si va a llamar a Catalogo, necesita un cliente de servicio en Authentication y su secreto en User Secrets.
5. **Documentación:**
   - README del sistema y una fila en el README raíz (servicios y puertos).
   - Una entrada en `sistemas.json` con sus capas, endpoints, dependencias y contratos.
6. **Commits** pequeños, en español, en la rama `feature/[nombre]`, sin push a `main`.

## Reglas heredadas del proyecto

- Clean Architecture: Domain → Application → Infrastructure → Api.
- CQRS con MediatR 12.5.0 y validación con FluentValidation.
- Errores con `GlobalExceptionHandler` y `ProblemDetails`.
- Sin acceso directo a la base de otro sistema.
