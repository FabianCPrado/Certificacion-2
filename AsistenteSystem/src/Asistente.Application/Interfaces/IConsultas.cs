using Asistente.Application.Asistente;

namespace Asistente.Application.Interfaces;

// Puertos de solo lectura hacia los otros servicios (regla 2). Infrastructure los implementa llamando a sus APIs.
// Un dato que no existe devuelve null o una lista vacía; un servicio caído lanza ServicioNoDisponibleException.

public interface ICatalogoConsultas
{
    Task<ProductoInfo?> ObtenerAsync(string categoria, string id, CancellationToken cancellationToken = default);

    /// <summary>Busca por nombre sin importar tildes ni mayúsculas; en una categoría o, si es null, en todas.</summary>
    Task<IReadOnlyList<ProductoInfo>> BuscarAsync(string? categoria, string nombre, CancellationToken cancellationToken = default);
}

public interface IPedidosConsultas
{
    /// <summary>Los últimos pedidos de un cliente, del más nuevo al más viejo, con sus líneas.</summary>
    Task<IReadOnlyList<PedidoInfo>> UltimosDeClienteAsync(Guid clienteId, int cantidad, CancellationToken cancellationToken = default);

    Task<PedidoInfo?> ObtenerAsync(Guid pedidoId, CancellationToken cancellationToken = default);
}

public interface IClientesConsultas
{
    Task<ClienteInfo?> ObtenerAsync(Guid clienteId, CancellationToken cancellationToken = default);
}
