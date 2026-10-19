namespace Pedidos.Application.Interfaces.Common;

/// <summary>
/// Caché clave-valor. Application solo conoce esta interfaz: que detrás haya Redis es un detalle de Infrastructure.
/// </summary>
public interface ICacheService
{
    /// <summary>El valor guardado bajo la clave, o nulo si no existe o ya venció.</summary>
    Task<T?> ObtenerAsync<T>(string clave, CancellationToken cancellationToken = default);

    /// <summary>Guarda el valor y lo deja vencer pasado <paramref name="duracion"/>.</summary>
    Task GuardarAsync<T>(string clave, T valor, TimeSpan duracion, CancellationToken cancellationToken = default);

    Task EliminarAsync(string clave, CancellationToken cancellationToken = default);
}
