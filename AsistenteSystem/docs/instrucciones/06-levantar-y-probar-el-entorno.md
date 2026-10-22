# Levantar y probar el entorno

> Cómo dejar los cinco sistemas funcionando en tu máquina y probar el asistente.

## 1. Bases de datos

Ejecutá los scripts de `sql/` en orden sobre `(localdb)\MSSQLLocalDB` (ver `sql/README.md`):

`01` Pedidos, `02` procedimientos, `03` Catalogo, `04` datos de prueba, `05` Clientes, `06` Authentication.

## 2. Secretos (una sola vez)

Los comandos están en `AuthenticationSystem/README.md`: `Jwt:SigningKey` (Authentication y Catalogo), `ClientCredentials:PedidosSecret`, `ClientCredentials:AsistenteSecret` y, para el asistente, `Authentication:ClientSecret` e `IA:ApiKey`.

## 3. Levantar los sistemas

Cada uno en su terminal (o con F5 en VS Code), **en este orden**:

```text
Authentication (7401) → Catalogo (7301) → Pedidos (7101) → Clientes (7201) → Asistente (7501)
```

Pedidos puede avisar de Cosmos (puerto 8083) si no tenés Docker corriendo: es solo para las demos de la clase 5 y no impide arrancar.

## 4. Verificar que está todo conectado

`GET https://localhost:7501/api/asistente/salud` debe devolver 200 con todos los componentes en `Ok`.

| Si dice... | Qué falta |
|---|---|
| `Authentication` en Error | Authentication no está corriendo, o falta el cliente `asistente-service` o su secreto |
| `Catalogo`: "Sin JWT" | Authentication no entrega el token |
| `Catalogo`: HTTP 403 | Falta la policy `LecturaCatalogo` en Catalogo |
| `Pedidos` o `Clientes` en Error | No están corriendo |
| `Modelo`: SinConfigurar | Falta `IA:ApiKey` |

## 5. Probar

- Swagger del Asistente: `https://localhost:7501/swagger` (el cuerpo de `preguntar` ya trae un cliente de ejemplo).
- O abrí `Asistente.http` en VS Code (extensión REST Client) y ejecutá las peticiones.

## Problemas conocidos

- Si un sistema está corriendo desde Visual Studio o VS Code, bloquea los DLL: detenelo antes de compilar desde la terminal.
- Una base creada antes de un cambio de esquema da 500 en las consultas nuevas: recreala con los scripts de `sql/`.
