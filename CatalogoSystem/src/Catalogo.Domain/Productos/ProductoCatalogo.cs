namespace Catalogo.Domain.Productos;

/// <summary>Producto del catálogo almacenado en SQL Server.</summary>
public class ProductoCatalogo
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Categoría del producto.</summary>
    public string Categoria { get; set; } = "";

    public string Nombre { get; set; } = "";

    public decimal Precio { get; set; }

    public int Stock { get; set; }

    public ICollection<ProductoAtributo> Atributos { get; set; } = new List<ProductoAtributo>();
}
