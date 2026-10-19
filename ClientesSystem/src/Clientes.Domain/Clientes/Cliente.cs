using Clientes.Domain.Common;

namespace Clientes.Domain.Clientes;

/// <summary>Cliente que hace pedidos: solo datos.</summary>
public sealed class Cliente() : Entity<Guid>(Guid.NewGuid())
{
    public string Nombre { get; set; } = null!;

    public string Email { get; set; } = null!;
}
