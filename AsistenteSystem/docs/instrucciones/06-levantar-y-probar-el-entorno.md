# Levantar y probar el entorno

> Cómo dejar los cuatro sistemas funcionando en tu máquina.

## 1. Bases de datos

Ejecutá los scripts de `sql/` en orden sobre `(localdb)\MSSQLLocalDB` (ver `sql/README.md`):

`01` Pedidos, `02` procedimientos, `03` Catalogo, `04` datos de prueba, `05` Clientes, `06` Authentication.

## 2. Secretos (una sola vez)

Los comandos están en `AuthenticationSystem/README.md`: `Jwt:SigningKey` (Authentication y Catalogo) y `ClientCredentials:PedidosSecret` (Authentication y Pedidos).

## 3. Levantar los sistemas

Cada uno en su terminal (o con F5 en VS Code), **en este orden**:

```text
Authentication (7401) → Catalogo (7301) → Pedidos (7101) → Clientes (7201)
```

Pedidos puede avisar de Cosmos (puerto 8083) si no tenés Docker corriendo: es solo para las demos de la clase 5 y no impide arrancar.

## 4. Verificar que está todo conectado

Abrí el Swagger de cada sistema (`https://localhost:<puerto>/swagger`) y probá un endpoint de lectura.

| Si pasa... | Qué falta |
|---|---|
| Authentication no responde | No está corriendo, o falta `ClientCredentials:PedidosSecret` |
| Catalogo responde 401 | Falta el token, o `Jwt:SigningKey` no es el mismo en Authentication y Catalogo |
| Catalogo responde 403 | El cliente de servicio no tiene permiso para esa operación |
| Pedidos responde 503 | Catalogo o Clientes no están corriendo |
| Clientes responde 503 al eliminar | Pedidos no está corriendo |

## Problemas conocidos

- Si un sistema está corriendo desde Visual Studio o VS Code, bloquea los DLL: detenelo antes de compilar desde la terminal.
- Una base creada antes de un cambio de esquema da 500 en las consultas nuevas: recreala con los scripts de `sql/`.
