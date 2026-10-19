using Catalogo.Domain.Productos;

namespace Catalogo.Application.Productos;

public record ProductoDto(string Id, string Categoria, string Nombre, decimal Precio, int Stock)
{
    public static ProductoDto Desde(ProductoCatalogo p) => new(p.Id, p.Categoria, p.Nombre, p.Precio, p.Stock);
}
