# Clientes · :7201

Fuente: `ClientesSystem/src/Clientes.Api/Controllers/ClientesController.cs`, `ClienteDto.cs` y `launchSettings.json`.

## Seguridad

Sin autenticación en las llamadas entrantes (decisión pendiente 1 de la especificación).

## Endpoints que usa el asistente (solo lectura)

| Herramienta | Ruta | Respuestas |
|---|---|---|
| `DatosDeCliente` | `GET /api/clientes/{id:guid}` | `200 ClienteDto`, `404` |

También existe `GET /api/clientes` (listado paginado), que el asistente no usa.

## ClienteDto

```csharp
record ClienteDto(Guid Id, string Nombre, string Email);
```

## Puntos que importan al asistente

- **La API devuelve `Email`.** La regla 6 de la especificación dice que nunca se manda al modelo. El adaptador `ClientesConsultasHttp` debe mapear a un tipo propio con `Id` y `Nombre` únicamente.
- Clientes **es el dueño** de estos datos: solo él los crea, cambia o elimina.
- `POST /api/clientes` existe pero el asistente no lo usa.
