# Plantilla para pedir endpoints a una IA

Minimal API en .NET 10, en `Catalogo.Api/Endpoints/ProductosEndpoints.cs`, grupo `/api/productos`.
Endpoint: <verbo y ruta> que <qué hace>.
- Usá MediatR (`ISender`) con una query o command nuevo en `Catalogo.Application/Productos`.
- Devolvé `TypedResults` con el tipo de retorno declarado (`Results<...>`).
- Recibí `CancellationToken` y pasalo a todo lo asíncrono.
- Devolvé DTOs, nunca entidades. Nada de lógica en el endpoint.
- Consultas a Cosmos con `QueryDefinition` y parámetros; nunca texto concatenado.
- Seguí el estilo de los endpoints que ya existen en el archivo.

## Checklist de revisión
- [ ] Valida la entrada
- [ ] Sin SQL ni consultas concatenadas; sin secretos
- [ ] Devuelve DTOs
- [ ] Maneja 404 y 400
- [ ] async y CancellationToken
- [ ] Sigue el estilo del proyecto
- [ ] Tiene al menos una prueba manual en Swagger

## Revisión

Pedido: `GET /api/productos/buscar?nombre=...` (búsqueda por nombre dentro de una categoría).

### Primera versión generada (con los problemas marcados)

```csharp
// ❌ 1. Sin CancellationToken
// ❌ 2. Results.Ok sin tipo: Swagger no sabe qué devuelve
// ❌ 3. Lógica y acceso a datos dentro del endpoint (se salta MediatR y Application)
// ❌ 4. Consulta concatenada: inyección de SQL
// ❌ 5. Devuelve la entidad ProductoCatalogo, no un DTO
// ❌ 6. No valida la entrada (nombre vacío o enorme)
g.MapGet("/buscar", async (string categoria, string nombre, CosmosClient cosmos) =>
{
    var c = cosmos.GetContainer("PedidosDb", "Productos");
    var q = new QueryDefinition($"SELECT * FROM c WHERE c.categoria = '{categoria}' AND CONTAINS(c.nombre, '{nombre}')");
    var it = c.GetItemQueryIterator<ProductoCatalogo>(q);
    var lista = new List<ProductoCatalogo>();
    while (it.HasMoreResults) lista.AddRange(await it.ReadNextAsync());
    return Results.Ok(lista);
});
```

### Qué se corrigió

| Problema | Corrección |
|---|---|
| Consulta concatenada | `QueryDefinition` con `@categoria` y `@nombre` en `CatalogoRepository.BuscarPorNombreAsync` |
| Lógica en el endpoint | El endpoint solo manda `BuscarProductosQuery` con `ISender`; el acceso a datos está detrás de `ICatalogoRepository` |
| Entidad expuesta | Devuelve `ProductoDto` |
| `Results.Ok` sin tipo | `Task<Ok<IReadOnlyList<ProductoDto>>>` con `TypedResults.Ok` |
| Sin `CancellationToken` | Se recibe y se pasa a `Send` y al iterador de Cosmos |
| Sin validación | `BuscarProductosQueryValidator`: categoría y nombre obligatorios, nombre de hasta 100 caracteres (si falla, 400 por el `ValidationBehavior`) |
| Búsqueda sensible a mayúsculas | `CONTAINS(c.nombre, @nombre, true)` ignora mayúsculas |

### Versión final

Ver `BuscarAsync` en `src/Catalogo.Api/Endpoints/ProductosEndpoints.cs`, `BuscarProductosQuery` en `Catalogo.Application/Productos/Queries/BuscarProductos` y `BuscarPorNombreAsync` en `Catalogo.Infrastructure/Cosmos/CatalogoRepository.cs`.

Prueba manual en Swagger: `GET /api/productos/buscar?categoria=Electro&nombre=tele` devuelve 200; con `nombre=` vacío devuelve 400.
