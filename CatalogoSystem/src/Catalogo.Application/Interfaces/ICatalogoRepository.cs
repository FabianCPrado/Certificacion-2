using Catalogo.Domain.Productos;

namespace Catalogo.Application.Interfaces;

/// <summary>Acceso al catálogo de productos (SQL Server). La define Application e Infrastructure la implementa.</summary>
public interface ICatalogoRepository
{
    /// <summary>Lectura puntual por id y categoría, o nulo si no existe.</summary>
    Task<ProductoCatalogo?> ObtenerAsync(string id, string categoria, CancellationToken cancellationToken = default);

    /// <summary>Una página de productos de una categoría.</summary>
    Task<IReadOnlyList<ProductoCatalogo>> PorCategoriaAsync(string categoria, int saltar, int tomar, CancellationToken cancellationToken = default);

    /// <summary>Productos de una categoría cuyo nombre contiene el texto buscado.</summary>
    Task<IReadOnlyList<ProductoCatalogo>> BuscarPorNombreAsync(string categoria, string nombre, CancellationToken cancellationToken = default);

    /// <summary>Crea el producto o reemplaza el que tenga el mismo id.</summary>
    Task GuardarAsync(ProductoCatalogo producto, CancellationToken cancellationToken = default);

    /// <summary>Elimina el producto. Devuelve false si no existía.</summary>
    Task<bool> EliminarAsync(string id, string categoria, CancellationToken cancellationToken = default);

    /// <summary>
    /// Suma <paramref name="delta"/> al stock en un solo UPDATE atómico (negativo descuenta). Devuelve false si el producto
    /// no existe o si el stock quedaría negativo.
    /// </summary>
    Task<bool> AjustarStockAsync(string id, string categoria, int delta, CancellationToken cancellationToken = default);

    /// <summary>Sube el precio de toda una categoría con un solo UPDATE. Devuelve las filas afectadas.</summary>
    Task<int> AumentarPrecioPorCategoriaAsync(string categoria, decimal porcentaje, CancellationToken cancellationToken = default);
}
