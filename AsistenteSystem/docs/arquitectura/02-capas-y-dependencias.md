# Capas y dependencias

> Cada sistema tiene cuatro proyectos (Domain, Application, Infrastructure, Api) y las dependencias solo van hacia adentro.

## La estructura de un sistema

```text
[Nombre]System/
  src/
    [Nombre].Domain          entidades, value objects, reglas de negocio
    [Nombre].Application     casos de uso (commands y queries), validadores, behaviors, interfaces
    [Nombre].Infrastructure  EF Core, HTTP/gRPC a otros sistemas, caché, almacenamiento
    [Nombre].Api             controladores o endpoints, Program.cs, manejo de errores
  tests/                     Domain.Tests, Application.Tests, Infrastructure.Tests
```

## Quién conoce a quién

```text
Api ──► Application ──► Domain
 │           ▲
 └──► Infrastructure ──┘   (Infrastructure implementa las interfaces de Application)
```

| Capa | Puede referenciar | No puede |
|---|---|---|
| Domain | nada | EF Core, HTTP, MediatR |
| Application | Domain | EF Core, HttpClient, SDKs de IA, Redis, Cosmos |
| Infrastructure | Application | exponer sus tipos hacia arriba |
| Api | Application e Infrastructure (solo para registrar servicios) | tener lógica |

## Reglas

1. **Application define interfaces; Infrastructure las implementa.** Ejemplo: `ICatalogoRepository` está en Application y `CatalogoRepository` (EF Core) en Infrastructure.
2. **Las tecnologías viven solo en Infrastructure.** Redis, Cosmos, Mongo y Blob no aparecen en Application ni en Domain.
3. **La lógica de negocio va en la entidad** (Domain), no en el controlador ni en el handler.
4. **Controladores y endpoints delgados:** reciben, mandan a MediatR y devuelven. Nada de reglas.
5. **Cada interfaz se registra en** `ApplicationServiceExtensions` o `InfrastructureServiceExtensions`.
6. **Textos de mensajes** en `Common/Mensajes` (en los sistemas que lo tienen), no repartidos por el código.

## Cómo saber si lo estás haciendo bien

- Si Application necesita un `using` de EF Core, `HttpClient` o un SDK, la interfaz está mal ubicada.
- Pedidos y Clientes tienen un test de arquitectura (`CqrsArquitecturaTests`) que vigila parte de esto.
