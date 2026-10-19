using Pedidos.Application.Productos.Queries.ObtenerProducto;

namespace Pedidos.Application.Interfaces.Productos;

/// <summary>
/// Los productos viven en otro servicio (CatalogoSystem). Application lo usa a través de esta interfaz, sin saber que
/// detrás hay HTTP: la implementación está en Infrastructure (Rest/Catalogo). Un producto se identifica por categoría + id.
/// </summary>
public interface ICatalogoProductos
{
    /// <summary>Un producto, o nulo si no existe.</summary>
    Task<ProductoDto?> ObtenerAsync(string categoria, string id, CancellationToken cancellationToken = default);

    /// <summary>Una página (de a 20) de los productos de una categoría.</summary>
    Task<IReadOnlyList<ProductoDto>> ListarAsync(string categoria, int pagina, CancellationToken cancellationToken = default);

    Task<ProductoDto> CrearAsync(
        string categoria, string nombre, decimal precio, int stock, CancellationToken cancellationToken = default);

    /// <summary>Cambia nombre, precio y stock. Lanza <c>NotFoundException</c> si no existe.</summary>
    Task ActualizarAsync(
        string categoria, string id, string nombre, decimal precio, int stock, CancellationToken cancellationToken = default);

    /// <summary>Cambia solo el precio. Lanza <c>NotFoundException</c> si no existe.</summary>
    Task CambiarPrecioAsync(string categoria, string id, decimal precio, CancellationToken cancellationToken = default);

    /// <summary>Lanza <c>NotFoundException</c> si no existe.</summary>
    Task EliminarAsync(string categoria, string id, CancellationToken cancellationToken = default);

    /// <summary>Sube el precio de toda una categoría. Devuelve cuántos productos cambió.</summary>
    Task<int> AumentarPrecioCategoriaAsync(string categoria, decimal porcentaje, CancellationToken cancellationToken = default);

    /// <summary>Descuenta stock de forma atómica. 404 si no existe; <c>ReglaNegocioException</c> si no alcanza.</summary>
    Task DescontarStockAsync(string categoria, string id, int cantidad, CancellationToken cancellationToken = default);

    /// <summary>Devuelve stock descontado antes (para compensar si falla un paso posterior).</summary>
    Task ReponerStockAsync(string categoria, string id, int cantidad, CancellationToken cancellationToken = default);
}
