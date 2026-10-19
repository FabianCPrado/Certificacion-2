namespace Pedidos.Domain.Catalogo;

/// <summary>
/// Producto del catálogo que vive en Cosmos DB: solo datos. Cada categoría tiene atributos distintos
/// (bebidas: litros; electro: pulgadas y voltaje…), por eso se guardan en un diccionario y no en columnas.
/// </summary>
public class ProductoCatalogo
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Clave de partición del contenedor (/categoria).</summary>
    public string Categoria { get; set; } = "";

    public string Nombre { get; set; } = "";

    public decimal Precio { get; set; }

    public Dictionary<string, string> Atributos { get; set; } = [];
}
