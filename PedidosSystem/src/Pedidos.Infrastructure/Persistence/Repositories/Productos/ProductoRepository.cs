using Microsoft.EntityFrameworkCore;
using Pedidos.Application.Interfaces.Productos;
using Pedidos.Domain.Common;
using Pedidos.Domain.Productos;

namespace Pedidos.Infrastructure.Persistence.Repositories.Productos;

public sealed class ProductoRepository(AppDbContext context) : IProductoRepository
{
    public Task<Producto?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Productos.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<Producto> CrearAsync(
        string nombre, string categoria, decimal precio, int stock, CancellationToken cancellationToken = default)
    {
        Validar(nombre, categoria, precio, stock);

        var producto = new Producto
        {
            Nombre = nombre.Trim(),
            Categoria = categoria.Trim(),
            Precio = precio,
            Stock = stock
        };
        context.Productos.Add(producto);

        await context.SaveChangesAsync(cancellationToken);
        return producto;
    }

    public async Task ActualizarAsync(
        Producto producto, string nombre, string categoria, decimal precio, int stock,
        CancellationToken cancellationToken = default)
    {
        Validar(nombre, categoria, precio, stock);

        producto.Nombre = nombre.Trim();
        producto.Categoria = categoria.Trim();
        producto.Precio = precio;
        producto.Stock = stock;

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task EliminarAsync(Producto producto, CancellationToken cancellationToken = default)
    {
        context.Productos.Remove(producto);
        await context.SaveChangesAsync(cancellationToken);
    }

    public Task<int> AumentarPrecioPorCategoriaAsync(
        string categoria, decimal porcentaje, CancellationToken cancellationToken = default)
    {
        if (porcentaje <= 0)
            throw new ReglaNegocioException("El porcentaje de aumento debe ser mayor que cero.");

        var factor = 1 + porcentaje / 100m;

        // Un solo UPDATE ... WHERE Categoria = ...: no se traen los productos a memoria
        return context.Productos
            .Where(p => p.Categoria == categoria)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Precio, p => p.Precio * factor), cancellationToken);
    }

    public async Task DescontarStockAsync(Producto producto, int cantidad, CancellationToken cancellationToken = default)
    {
        if (cantidad <= 0)
            throw new ReglaNegocioException("La cantidad debe ser mayor que cero.");

        if (producto.Stock < cantidad)
            throw new ReglaNegocioException("No hay stock suficiente.");

        await context.Database.ExecuteSqlAsync($"EXEC ActualizarStock {producto.Id}, {cantidad}", cancellationToken);
        producto.Stock -= cantidad;
    }

    private static void Validar(string nombre, string categoria, decimal precio, int stock)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ReglaNegocioException("El nombre del producto es obligatorio.");

        if (string.IsNullOrWhiteSpace(categoria))
            throw new ReglaNegocioException("La categoría del producto es obligatoria.");

        if (precio < 0)
            throw new ReglaNegocioException("El precio no puede ser negativo.");

        if (stock < 0)
            throw new ReglaNegocioException("El stock no puede ser negativo.");
    }
}
