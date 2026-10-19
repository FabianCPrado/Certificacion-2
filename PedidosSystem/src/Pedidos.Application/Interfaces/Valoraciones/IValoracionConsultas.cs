using Pedidos.Application.Valoraciones.Queries.ListarValoraciones;

namespace Pedidos.Application.Interfaces.Valoraciones;

/// <summary>Consultas de solo lectura de valoraciones: proyectan directo al DTO.</summary>
public interface IValoracionConsultas
{
    /// <summary>Valoraciones de un producto, de la más nueva a la más vieja.</summary>
    Task<IReadOnlyList<ValoracionDto>> ListarPorProductoAsync(Guid productoId, CancellationToken cancellationToken = default);
}
