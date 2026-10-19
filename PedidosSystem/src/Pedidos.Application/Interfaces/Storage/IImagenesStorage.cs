namespace Pedidos.Application.Interfaces.Storage;

/// <summary>Una imagen guardada: su nombre dentro del contenedor y su URL.</summary>
public sealed record ImagenDto(string Nombre, string Url);

/// <summary>Guarda las imágenes de los productos. La define Application e Infrastructure la implementa (Blob Storage).</summary>
public interface IImagenesStorage
{
    /// <summary>Sube el archivo y devuelve su URL: la base de datos guarda solo esa dirección.</summary>
    Task<string> SubirAsync(string nombre, Stream contenido, string contentType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista las imágenes cuyo nombre empieza con <paramref name="prefijo"/>. Un producto puede tener varias:
    /// se guardan como "{productoId}/{archivo}" y se buscan con el prefijo "{productoId}/".
    /// </summary>
    Task<IReadOnlyList<ImagenDto>> ListarAsync(string prefijo, CancellationToken cancellationToken = default);
}
