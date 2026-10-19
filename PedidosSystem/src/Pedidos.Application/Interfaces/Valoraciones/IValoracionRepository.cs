namespace Pedidos.Application.Interfaces.Valoraciones;

/// <summary>Datos de una valoración nueva. Solo transporta datos.</summary>
public sealed record NuevaValoracion(Guid ProductoId, string Usuario, int Puntaje, string? Comentario);

/// <summary>Guarda valoraciones de productos. La define Application e Infrastructure la implementa.</summary>
public interface IValoracionRepository
{
    /// <summary>Guarda la valoración y devuelve su Id.</summary>
    Task<Guid> CrearAsync(NuevaValoracion valoracion, CancellationToken cancellationToken = default);
}
