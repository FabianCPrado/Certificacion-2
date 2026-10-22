# Pruebas y calidad

> Cada capa se prueba por separado, con dobles de prueba en vez de servicios reales.

## Qué se prueba y dónde

| Proyecto | Qué prueba |
|---|---|
| `[Nombre].Domain.Tests` | Reglas de negocio de las entidades y value objects |
| `[Nombre].Application.Tests` | Handlers, validadores y la arquitectura CQRS (`CqrsArquitecturaTests`) |
| `[Nombre].Infrastructure.Tests` | Consultas EF Core y adaptadores a otros sistemas |

Pedidos y Clientes tienen los tres proyectos de tests. Authentication y Catalogo todavía no tienen.

## Reglas

1. **Dobles de prueba para lo externo.** Un servidor HTTP falso (`HttpMessageHandler`) y `NSubstitute` para las interfaces.
2. **Si agregás un método a una interfaz**, actualizá los fakes de los tests que la implementan.
3. **Cada comportamiento nuevo trae su test.** Un bug corregido trae el test que lo habría detectado.

## Calidad

- `TreatWarningsAsErrors` y SonarAnalyzer: un aviso rompe la compilación. Ejemplo real: la palabra "todo" en un comentario se detecta como una tarea pendiente.
- Comandos: `dotnet build` y `dotnet test` en la carpeta del sistema, sin errores antes de cada commit.
- Si Visual Studio o VS Code tiene la API corriendo, bloquea los DLL: hay que detenerla antes de compilar desde la terminal.
