namespace Pedidos.Infrastructure.Cosmos;

/// <summary>
/// Cómo se guarda una valoración en Cosmos DB. Es un detalle de Infrastructure.
/// Cosmos exige una propiedad "id" (string) y usa <see cref="ProductoId"/> como clave de partición (/productoId):
/// todas las valoraciones de un producto quedan juntas y se leen en una sola partición.
/// </summary>
public sealed class ValoracionDocumento
{
    public string Id { get; set; } = null!;

    public string ProductoId { get; set; } = null!;

    public string Usuario { get; set; } = null!;

    public int Puntaje { get; set; }

    public string? Comentario { get; set; }

    public DateTime FechaUtc { get; set; }
}
