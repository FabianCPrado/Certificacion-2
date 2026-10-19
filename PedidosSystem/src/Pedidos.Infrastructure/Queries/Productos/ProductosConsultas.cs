using Microsoft.EntityFrameworkCore;
using Pedidos.Application.Common;
using Pedidos.Application.Interfaces.Productos;
using Pedidos.Application.Productos.Queries.ObtenerProducto;
using Pedidos.Domain.Productos;
using Pedidos.Infrastructure.Persistence;

namespace Pedidos.Infrastructure.Queries.Productos;

public sealed class ProductosConsultas(AppDbContext context) : IProductoConsultas
{
    public Task<ProductoDto?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Productos.AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new ProductoDto(p.Id, p.Nombre, p.Categoria, p.Precio, p.Stock))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<PagedResult<ProductoDto>> ListarAsync(
        int pagina, int tamanio, string? categoria, decimal? precioMinimo,
        CancellationToken cancellationToken = default)
    {
        var filtrados = Filtrar(categoria, precioMinimo);

        var total = await filtrados.CountAsync(cancellationToken);
        var items = await Paginar(filtrados, pagina, tamanio).ToListAsync(cancellationToken);

        return new PagedResult<ProductoDto>(items, pagina, tamanio, total);
    }

    public async Task<IReadOnlyList<ProductoDto>> BuscarPorNombreAsync(
        string nombre, CancellationToken cancellationToken = default) =>
        // FromSql con interpolación: el valor viaja como parámetro (@p0), nunca dentro del texto SQL
        // RelationalQueryableExtensions explícito: el proveedor de EF Core para Cosmos (Clase 5) define otro FromSql y la llamada sería ambigua
        await RelationalQueryableExtensions.FromSql(context.Productos, $"SELECT * FROM Productos WHERE Nombre = {nombre}")
            .AsNoTracking()
            .Select(p => new ProductoDto(p.Id, p.Nombre, p.Categoria, p.Precio, p.Stock))
            .ToListAsync(cancellationToken);

    public string ObtenerSqlListado(int pagina, int tamanio, string? categoria, decimal? precioMinimo) =>
        Paginar(Filtrar(categoria, precioMinimo), pagina, tamanio).ToQueryString();

    // El filtro se arma sobre IQueryable: no se ejecuta nada hasta el final, y la base hace el trabajo
    private IQueryable<Producto> Filtrar(string? categoria, decimal? precioMinimo)
    {
        var consulta = context.Productos.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(categoria))
            consulta = consulta.Where(p => p.Categoria == categoria);

        if (precioMinimo is not null)
            consulta = consulta.Where(p => p.Precio >= precioMinimo);

        return consulta;
    }

    private static IQueryable<ProductoDto> Paginar(IQueryable<Producto> consulta, int pagina, int tamanio) =>
        consulta
            .OrderBy(p => p.Nombre)
            .Skip((pagina - 1) * tamanio).Take(tamanio)
            .Select(p => new ProductoDto(p.Id, p.Nombre, p.Categoria, p.Precio, p.Stock));
}
