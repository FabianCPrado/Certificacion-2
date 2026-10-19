using Pedidos.Domain.Productos;

namespace Pedidos.Application.Interfaces.Productos;

/// <summary>
/// Repositorio del agregado Producto: la interfaz la define Application (lo que necesita)
/// y la implementación, con las reglas de negocio, vive en Infrastructure.
/// </summary>
public interface IProductoRepository
{
    Task<Producto?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Valida las reglas (nombre y categoría obligatorios, precio y stock no negativos) y persiste.</summary>
    Task<Producto> CrearAsync(
        string nombre, string categoria, decimal precio, int stock, CancellationToken cancellationToken = default);

    /// <summary>Valida las reglas y persiste los cambios del producto.</summary>
    Task ActualizarAsync(
        Producto producto, string nombre, string categoria, decimal precio, int stock,
        CancellationToken cancellationToken = default);

    Task EliminarAsync(Producto producto, CancellationToken cancellationToken = default);

    /// <summary>Sube el precio de toda una categoría con un solo UPDATE, sin traer los productos. Devuelve las filas afectadas.</summary>
    Task<int> AumentarPrecioPorCategoriaAsync(
        string categoria, decimal porcentaje, CancellationToken cancellationToken = default);

    /// <summary>Descuenta stock con el procedimiento almacenado, validando que alcance.</summary>
    Task DescontarStockAsync(Producto producto, int cantidad, CancellationToken cancellationToken = default);
}
