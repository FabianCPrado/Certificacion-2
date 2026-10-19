using Clientes.Application.Clientes.Queries.ObtenerCliente;
using Clientes.Application.Common;

namespace Clientes.Application.Interfaces.Clientes;

/// <summary>Consultas de solo lectura de clientes: proyectan directo al DTO, sin cargar entidades.</summary>
public interface IClienteConsultas
{
    /// <summary>Un cliente, o nulo si no existe.</summary>
    Task<ClienteDto?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<ClienteDto>> ListarAsync(int pagina, int tamanio, CancellationToken cancellationToken = default);
}
