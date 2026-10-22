using Catalogo.Application.Interfaces;
using Catalogo.Domain.Productos;
using Microsoft.EntityFrameworkCore;

namespace Catalogo.Infrastructure.Persistence;

/// <summary>Catálogo de productos en SQL Server con EF Core.</summary>
public sealed class CatalogoRepository(CatalogoDbContext db) : ICatalogoRepository
{
    public async Task<ProductoCatalogo?> ObtenerAsync(string id, string categoria, CancellationToken cancellationToken = default) =>
        await db.Productos.AsNoTracking()
            .Include(p => p.Atributos)
            .FirstOrDefaultAsync(p => p.Id == id && p.Categoria == categoria, cancellationToken);

    public async Task<IReadOnlyList<ProductoCatalogo>> PorCategoriaAsync(string categoria, int saltar, int tomar, CancellationToken cancellationToken = default) =>
        await db.Productos.AsNoTracking()
            .Include(p => p.Atributos)
            .Where(p => p.Categoria == categoria)
            .OrderBy(p => p.Nombre)
            .Skip(saltar).Take(tomar)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<string>> CategoriasAsync(CancellationToken cancellationToken = default) =>
        await db.Productos.AsNoTracking()
            .Select(p => p.Categoria)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ProductoCatalogo>> TodosAsync(int saltar, int tomar, CancellationToken cancellationToken = default) =>
        await db.Productos.AsNoTracking()
            .Include(p => p.Atributos)
            .OrderBy(p => p.Categoria).ThenBy(p => p.Nombre).ThenBy(p => p.Id)
            .Skip(saltar).Take(tomar)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ProductoCatalogo>> BuscarPorNombreAsync(string categoria, string nombre, CancellationToken cancellationToken = default) =>
        await db.Productos.AsNoTracking()
            .Include(p => p.Atributos)
            .Where(p => p.Categoria == categoria && EF.Functions.Like(p.Nombre, $"%{nombre}%"))
            .ToListAsync(cancellationToken);

    public async Task GuardarAsync(ProductoCatalogo producto, CancellationToken cancellationToken = default)
    {
        var existe = await db.Productos.AnyAsync(p => p.Id == producto.Id, cancellationToken);
        if (existe) db.Productos.Update(producto);
        else db.Productos.Add(producto);

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> EliminarAsync(string id, string categoria, CancellationToken cancellationToken = default) =>
        await db.Productos.Where(p => p.Id == id && p.Categoria == categoria).ExecuteDeleteAsync(cancellationToken) > 0;

    public async Task<bool> AjustarStockAsync(string id, string categoria, int delta, CancellationToken cancellationToken = default) =>
        await db.Productos
            .Where(p => p.Id == id && p.Categoria == categoria && p.Stock + delta >= 0)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock + delta), cancellationToken) > 0;

    public async Task<int> AumentarPrecioPorCategoriaAsync(string categoria, decimal porcentaje, CancellationToken cancellationToken = default) =>
        await db.Productos
            .Where(p => p.Categoria == categoria)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Precio, p => p.Precio * (1 + porcentaje / 100m)), cancellationToken);
}
