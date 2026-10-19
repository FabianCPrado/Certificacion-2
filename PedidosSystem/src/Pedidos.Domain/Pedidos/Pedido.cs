using Pedidos.Domain.Common;
using Pedidos.Domain.ValueObjects;

namespace Pedidos.Domain.Pedidos;

/// <summary>Entidad Pedido: solo datos. Las reglas viven en la capa Application.</summary>
public sealed class Pedido() : Entity<Guid>(Guid.NewGuid())
{
    public Guid ClienteId { get; set; }

    public Email EmailCliente { get; set; } = null!;

    public Direccion DireccionEnvio { get; set; } = null!;

    public string Moneda { get; set; } = null!;

    public EstadoPedido Estado { get; set; }

    public DateTime CreadoEn { get; set; }

    public List<LineaPedido> Lineas { get; set; } = [];
}
