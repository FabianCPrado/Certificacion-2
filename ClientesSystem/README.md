# ClientesSystem — Servicio Clientes

Microservicio independiente que se separó de `PedidosSystem`. Tiene su **propia solución y su propia base (`ClientesDb`)** en la misma instancia de SQL Server (LocalDB). `PedidosSystem` lo consumirá por REST.

Mantiene **la misma estructura que PedidosSystem**: Clean Architecture, CQRS con MediatR 12.5.0, FluentValidation, EF Core y `GlobalExceptionHandler` con `ProblemDetails`. .NET 10.

## Estructura

```
ClientesSystem/
├── src/
│   ├── Clientes.Domain/           Cliente, Email, Entity, ReglaNegocioException
│   ├── Clientes.Application/      Commands/Queries de Clientes, Behaviors, Interfaces (IClienteRepository,
│   │                              IClienteConsultas, IAppDbContext)  → AddApplication()
│   ├── Clientes.Infrastructure/   AppDbContext, ClienteMapeo, ClienteRepository, ClientesConsultas
│   │                              → AddInfrastructure()
│   └── Clientes.Api/              ClientesController, GlobalExceptionHandler, Program.cs
├── tests/
│   ├── Clientes.Domain.Tests/        Email
│   └── Clientes.Application.Tests/   handlers, validadores y reglas de CQRS
└── scripts/crear-base-de-datos.sql
```

Dependencias: `Api → Application, Infrastructure` · `Infrastructure → Application` · `Application → Domain`.

## Cómo correrlo

```powershell
# 1. Crear la base ClientesDb (copia los clientes que ya existan en PedidosDb, con el mismo Id)
#    sqlcmd -S "(localdb)\MSSQLLocalDB" -E -i scripts\crear-base-de-datos.sql

# 2. Compilar y probar
dotnet build
dotnet test

# 3. Ejecutar la API → Swagger en https://localhost:7201/swagger
dotnet run --project src/Clientes.Api --launch-profile https
```

Puertos: Clientes `7201/5201` · Pedidos `7101/5101`. Cadena de conexión `Clientes` en `appsettings.Development.json`.

## Contrato REST (el que consumirá Pedidos)

| Método | Ruta | Respuestas |
|---|---|---|
| POST | `/api/clientes` `{ nombre, email }` | 201 (devuelve el id), 400, 409 (email duplicado) |
| GET | `/api/clientes?pagina=&tamanio=` | 200, 400 |
| GET | `/api/clientes/{id}` | 200, 404 |
| PUT | `/api/clientes/{id}` `{ nombre, email }` | 204, 400, 404, 409 |
| DELETE | `/api/clientes/{id}` | 204, 404, 409 (tiene pedidos), 503 (Pedidos no responde) |

## Diferencias frente a Pedidos

- Esta API es la **única que hace el CRUD** de clientes; Pedidos solo los consulta (GET) al momento.
- La regla "no se elimina un cliente con pedidos" se aplica aquí **preguntándole a Pedidos por REST** (`GET api/pedidos?clienteId=…&tamanio=1`, mira el `total`) desde `ClienteRepository.EliminarAsync`, a través de la interfaz `IPedidosConsultas` (implementada en `Infrastructure/Rest/Pedidos`). Con pedidos → **409**. Si Pedidos no responde → **503** y el cliente **no se borra** (falla cerrado).
- URL de Pedidos en `Servicios:Pedidos` (`https://localhost:7101/`). Para eliminar clientes hay que tener Pedidos arriba; el resto del CRUD no lo necesita.
- Los `Id` se conservan: `Pedido.ClienteId` sigue apuntando al mismo cliente.
