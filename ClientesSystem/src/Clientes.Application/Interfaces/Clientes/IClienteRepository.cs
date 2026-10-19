using Clientes.Domain.Clientes;
using Clientes.Domain.ValueObjects;

namespace Clientes.Application.Interfaces.Clientes;

/// <summary>
/// Repositorio del agregado Cliente: la interfaz la define Application (lo que necesita)
/// y la implementación, con las reglas de negocio, vive en Infrastructure.
/// </summary>
public interface IClienteRepository
{
    Task<Cliente?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Valida las reglas (nombre obligatorio, email único) y persiste el cliente.</summary>
    Task<Cliente> CrearAsync(string nombre, Email email, CancellationToken cancellationToken = default);

    /// <summary>Valida las reglas y persiste los cambios del cliente.</summary>
    Task ActualizarAsync(Cliente cliente, string nombre, Email email, CancellationToken cancellationToken = default);

    /// <summary>Elimina el cliente, siempre que no tenga pedidos.</summary>
    Task EliminarAsync(Cliente cliente, CancellationToken cancellationToken = default);
}
