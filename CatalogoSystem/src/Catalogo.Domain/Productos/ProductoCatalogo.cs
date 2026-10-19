namespace Catalogo.Domain.Productos;

/// <summary>
/// Producto del catálogo que vive en SQL Server: solo datos. Cada categoría tiene atributos distintos
/// (bebidas: litros; electro: pulgadas y voltaje…), por eso se guardan en un diccionario y no en columnas.
/// </summary>
public class ProductoCatalogo
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Categoría del producto.</summary>
    public string Categoria { get; set; } = "";

    public string Nombre { get; set; } = "";

    public decimal Precio { get; set; }

    public int Stock { get; set; }

    public Dictionary<string, string> Atributos { get; set; } = [];
}
