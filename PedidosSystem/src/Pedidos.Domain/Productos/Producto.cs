using Pedidos.Domain.Common;

namespace Pedidos.Domain.Productos;

/// <summary>Producto del catálogo: solo datos.</summary>
public sealed class Producto() : Entity<Guid>(Guid.NewGuid())
{
    public string Nombre { get; set; } = null!;

    public string Categoria { get; set; } = null!;

    public decimal Precio { get; set; }

    public int Stock { get; set; }
}
