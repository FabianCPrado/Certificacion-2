# Pruebas y calidad

> Cada capa se prueba por separado, con fakes en vez de servicios reales. Los tests nunca llaman a la IA ni gastan dinero.

## Qué se prueba y dónde

| Proyecto | Qué prueba |
|---|---|
| `[Nombre].Domain.Tests` | Reglas de negocio de las entidades y value objects |
| `[Nombre].Application.Tests` | Handlers, validadores y la arquitectura CQRS (`CqrsArquitecturaTests`) |
| `[Nombre].Infrastructure.Tests` | Consultas EF Core y adaptadores a otros sistemas |
| `Asistente.Tests` | Herramientas, el asistente con un modelo falso, el token y los adaptadores HTTP |
| `Asistente.IntegrationTests` | Los endpoints reales con `WebApplicationFactory` |

Pedidos y Clientes tienen los tres proyectos de tests. Authentication y Catalogo todavía no tienen.

## Reglas

1. **Dobles de prueba para lo externo.** Un modelo falso (`IChatClient`), un servidor HTTP falso (`HttpMessageHandler`) y `NSubstitute` para las interfaces.
2. **Los tests de integración no llaman a Claude.** Se fuerza `IA:ApiKey` vacía, aunque el equipo tenga la clave en User Secrets.
3. **Si agregás un método a una interfaz**, actualizá los fakes de los tests que la implementan.
4. **Cada comportamiento nuevo trae su test.** Un bug corregido trae el test que lo habría detectado.

## Calidad

- `TreatWarningsAsErrors` y SonarAnalyzer: un aviso rompe la compilación. Ejemplo real: la palabra "todo" en un comentario se detecta como una tarea pendiente.
- Comandos: `dotnet build` y `dotnet test` en la carpeta del sistema, sin errores antes de cada commit.
- Si Visual Studio o VS Code tiene la API corriendo, bloquea los DLL: hay que detenerla antes de compilar desde la terminal.
