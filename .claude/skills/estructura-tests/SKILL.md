---
name: estructura-tests
description: Dónde va cada test, fake y namespace en el proyecto de tests de AsistenteSystem (tests/Asistente.Tests). Úsalo al agregar, mover o revisar tests, o al crear un fake, para respetar la estructura que espeja a src/.
---

# Estructura de `tests/Asistente.Tests`

Las carpetas **espejan las capas de `src/`**: quien conoce `src` encuentra el test en el mismo lugar. El namespace sigue a la carpeta (`Asistente.Tests.<carpeta>`).

```
tests/Asistente.Tests/
├── Application/                    ← prueba src/Asistente.Application
│   ├── Fakes/                      ← fakes de interfaces definidas en Application
│   ├── Asistente/                  ← validadores de Application/Asistente
│   └── Sistemas/                   ← ConocimientoEcosistema, validadores de Sistemas
├── Infrastructure/                 ← prueba src/Asistente.Infrastructure
│   ├── Fakes/                      ← fakes de HTTP, modelo, token, reloj
│   ├── IA/                         ← AsistenteIA, SistemasIA, Herramientas
│   ├── Mapa/                       ← EstructuraSistemas, BibliotecaDocumentos, MapaSistema
│   └── Servicios/                  ← ConsultasHttp, TokenProvider, VerificadorSalud
└── Contratos/                      ← coherencia de los archivos reales del repo
    ├── RepositorioReal.cs          ← ubica la raíz y los archivos reales
    └── EcosistemaRealTests.cs
```

## Reglas

1. **Un archivo de test por clase de `src`**, con el nombre `<ClaseDeSrc>Tests` (`VerificadorSalud` → `VerificadorSaludTests`). La carpeta es la misma que la de la clase en `src`.
2. **El test va en la capa de la clase que prueba**, no donde "se siente" más cómodo. Ojo: `ConocimientoEcosistema` vive en `Application/Sistemas`, así que su test también (aunque use fakes de datos).
3. **El fake va en `Fakes/` de la capa donde está definida la interfaz o el tipo que imita.** `IMapaSistema`, `IEstructuraSistemas`, `IBibliotecaDocumentos` → `Application/Fakes`. `IChatClient`, `HttpMessageHandler`, `ITokenProvider`, `TimeProvider` → `Infrastructure/Fakes`.
4. **Dirección de dependencias:** un test de Infrastructure puede usar fakes de Application; un test de Application nunca usa nada de Infrastructure.
5. **Un fake por archivo**, `internal sealed`, nombrado en español con sufijo `Falso`/`Falsa` (`ModeloFalso`, `EstructuraFalsa`), o por lo que hace (`TokenQueFalla`). Nunca dentro del archivo de un test.
6. **`Contratos/` es para tests que no prueban una clase** sino que el repo es coherente consigo mismo (`sistemas.json` contra `launchSettings`, docs sin menciones prohibidas, mapa real contra el código). Si un test lee un archivo real pero se centra en una clase (p. ej. `BibliotecaDocumentos` leyendo `docs/`), va junto a su clase y usa `Contratos/RepositorioReal`. Los tests que leen archivos reales salen sin fallar (`if (RepositorioReal.Raiz() is null) return;`) cuando no están en el repo completo.
7. **Nombres de métodos** en español, en frase que dice el comportamiento (`Un_sistema_inexistente_falla_y_lista_los_validos`). Se mantienen `[Fact]`/`[Theory]` de xUnit y NSubstitute para interfaces simples.

## Trampa de namespaces

Como los namespaces son `Asistente.Tests.Application.*` e `Asistente.Tests.Infrastructure.*`, **dentro del código** una referencia relativa como `Infrastructure.IA.Herramientas` o `Asistente.Domain.X` puede resolver al namespace de los tests y no compilar. Importa con `using Asistente.Infrastructure.IA;` arriba del archivo y usa el nombre simple (`Herramientas`). Los `using` van antes del `namespace` de archivo, ahí no hay problema.

## Al agregar un test, en orden

1. Identifica la clase de `src` y su carpeta → ese es el espejo.
2. Si el archivo `<Clase>Tests.cs` existe, agrega ahí; si no, créalo con el namespace de la carpeta.
3. Si necesitas un fake nuevo, aplica la regla 3 y créalo en su archivo propio.
4. Si el test no prueba una clase sino un acuerdo entre archivos del repo, va a `Contratos/`.
5. Ejecuta `dotnet test tests/Asistente.Tests` desde `AsistenteSystem/`. Al reorganizar, el total de pruebas debe quedar igual que antes (hoy: 69).

## Fuera de alcance

`tests/Asistente.IntegrationTests` (un solo archivo, `AsistenteApiTests.cs`) no sigue este espejo: prueba el API completo con `WebApplicationFactory`.

Commits: uno por sistema, en español, con `fabiancprado@gmail.com`.
