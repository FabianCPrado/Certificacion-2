using Pedidos.Application.Common;
using Pedidos.Application.Productos.Queries.ObtenerProducto;

namespace Pedidos.Application.Interfaces.Productos;

/// <summary>
/// Consultas de solo lectura de productos: filtran, ordenan y paginan en la base,
/// y proyectan directo al DTO (sin tracking ni entidades).
/// </summary>
public interface IProductoConsultas
{
    /// <summary>Un producto, o nulo si no existe.</summary>
    Task<ProductoDto?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<ProductoDto>> ListarAsync(
        int pagina, int tamanio, string? categoria, decimal? precioMinimo,
        CancellationToken cancellationToken = default);

    /// <summary>Busca por nombre exacto con SQL directo y el valor como parámetro (seguro frente a inyección).</summary>
    Task<IReadOnlyList<ProductoDto>> BuscarPorNombreAsync(string nombre, CancellationToken cancellationToken = default);

    /// <summary>Devuelve el SQL que generaría el listado, sin ejecutarlo (para ver qué manda EF Core).</summary>
    string ObtenerSqlListado(int pagina, int tamanio, string? categoria, decimal? precioMinimo);
}
