using Microsoft.EntityFrameworkCore;
using Pedidos.Application.Interfaces.Demo;
using Pedidos.Domain.Catalogo;

namespace Pedidos.Infrastructure.Cosmos;

/// <summary>Demo de la Clase 5 con el proveedor de EF Core para Cosmos DB: AddRange + SaveChanges y LINQ, como en SQL.</summary>
public sealed class DemoCatalogoEf(CatalogoDbContext db) : IDemoCatalogoEf
{
    private const int Total = 30;

    public async Task<int> CargarAsync(CancellationToken cancellationToken = default)
    {
        // Cada llamada agrega 30 productos nuevos (ids nuevos): igual que en SQL, AddRange y un solo SaveChanges
        db.Productos.AddRange(Enumerable.Range(1, Total).Select(i => new ProductoCatalogo
        {
            Categoria = i % 2 == 0 ? "Bebidas" : "Electro",
            Nombre = $"EF {i}",
            Precio = 500 + i,
            Atributos = i % 2 == 0 ? new() { ["litros"] = "2" } : new() { ["pulgadas"] = "32" }
        }));

        await db.SaveChangesAsync(cancellationToken);
        return Total;
    }

    public async Task<IReadOnlyList<ProductoCatalogo>> ListarAsync(string categoria, CancellationToken cancellationToken = default) =>
        await db.Productos
            .WithPartitionKey(categoria)
            .OrderBy(p => p.Nombre)
            .ToListAsync(cancellationToken);
}
