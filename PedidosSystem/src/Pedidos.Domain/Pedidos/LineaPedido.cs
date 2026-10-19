using Pedidos.Domain.Common;
using Pedidos.Domain.ValueObjects;

namespace Pedidos.Domain.Pedidos;

/// <summary>Entidad línea de un pedido: solo datos.</summary>
public sealed class LineaPedido() : Entity<Guid>(Guid.NewGuid())
{
    public Guid ProductoId { get; set; }

    public string NombreProducto { get; set; } = null!;

    public Dinero PrecioUnitario { get; set; } = null!;

    public int Cantidad { get; set; }
}
