using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Pedidos.Application.Interfaces.Storage;

namespace Pedidos.Infrastructure.Storage;

/// <summary>Guarda las imágenes en Blob Storage (Azurite en local). La base de datos guarda solo la URL que devuelve.</summary>
public sealed class BlobImagenesStorage(BlobServiceClient blobs) : IImagenesStorage
{
    private const string Contenedor = "productos";

    public async Task<string> SubirAsync(
        string nombre, Stream contenido, string contentType, CancellationToken cancellationToken = default)
    {
        var contenedor = blobs.GetBlobContainerClient(Contenedor);
        await contenedor.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);

        var blob = contenedor.GetBlobClient(nombre);
        await blob.UploadAsync(
            contenido,
            new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } },
            cancellationToken);

        return blob.Uri.ToString();
    }

    public async Task<IReadOnlyList<ImagenDto>> ListarAsync(string prefijo, CancellationToken cancellationToken = default)
    {
        var contenedor = blobs.GetBlobContainerClient(Contenedor);

        // Si todavía no se subió ninguna imagen, el contenedor no existe
        if (!await contenedor.ExistsAsync(cancellationToken))
            return [];

        // Blob no tiene carpetas de verdad: el "/" del nombre es solo parte del texto, y buscar por prefijo
        // lo resuelve el servicio de almacenamiento sin recorrer el contenedor completo
        var nombres = new List<string>();
        await foreach (var blob in contenedor.GetBlobsAsync(BlobTraits.None, BlobStates.None, prefijo, cancellationToken))
            nombres.Add(blob.Name);

        return nombres.Select(nombre => new ImagenDto(nombre, contenedor.GetBlobClient(nombre).Uri.ToString())).ToList();
    }
}
