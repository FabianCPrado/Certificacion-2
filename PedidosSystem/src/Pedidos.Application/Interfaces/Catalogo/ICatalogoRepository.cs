using Pedidos.Domain.Catalogo;

namespace Pedidos.Application.Interfaces.Catalogo;

/// <summary>Acceso al catálogo de productos (Cosmos DB). La define Application e Infrastructure la implementa.</summary>
public interface ICatalogoRepository
{
    /// <summary>Lectura puntual por id y categoría (la operación más barata de Cosmos), o nulo si no existe.</summary>
    Task<ProductoCatalogo?> ObtenerAsync(string id, string categoria, CancellationToken cancellationToken = default);

    /// <summary>Productos de una categoría: la consulta se resuelve dentro de una sola partición.</summary>
    Task<IReadOnlyList<ProductoCatalogo>> PorCategoriaAsync(string categoria, CancellationToken cancellationToken = default);

    /// <summary>Crea el producto o reemplaza el que tenga el mismo id.</summary>
    Task GuardarAsync(ProductoCatalogo producto, CancellationToken cancellationToken = default);
}
