using Pedidos.Domain.Common;
using Pedidos.Domain.ValueObjects;

namespace Pedidos.Domain.Pedidos;

/// <summary>Entidad línea de un pedido: solo datos. El producto vive en el servicio Catálogo y se identifica por categoría + id.</summary>
public sealed class LineaPedido() : Entity<Guid>(Guid.NewGuid())
{
    public string ProductoId { get; set; } = null!;

    public string Categoria { get; set; } = null!;

    public string NombreProducto { get; set; } = null!;

    public Dinero PrecioUnitario { get; set; } = null!;

    public int Cantidad { get; set; }
}
