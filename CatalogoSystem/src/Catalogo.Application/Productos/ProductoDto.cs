using Catalogo.Domain.Productos;

namespace Catalogo.Application.Productos;

public record AtributoDto(string Parametro, string Valor);

public record ProductoDto(string Id, string Categoria, string Nombre, decimal Precio, int Stock, IReadOnlyList<AtributoDto> Atributos)
{
    public static ProductoDto Desde(ProductoCatalogo p) => new(
        p.Id,
        p.Categoria,
        p.Nombre,
        p.Precio,
        p.Stock,
        p.Atributos.Select(a => new AtributoDto(a.Parametro, a.Valor)).ToList());
}
