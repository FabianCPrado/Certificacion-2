using Microsoft.Azure.Cosmos;
using Pedidos.Application.Interfaces.Valoraciones;

namespace Pedidos.Infrastructure.Cosmos;

/// <summary>Guarda valoraciones en Cosmos DB. Application solo conoce <see cref="IValoracionRepository"/>.</summary>
public sealed class ValoracionCosmosRepository(CosmosValoraciones cosmos, TimeProvider tiempo) : IValoracionRepository
{
    public async Task<Guid> CrearAsync(NuevaValoracion valoracion, CancellationToken cancellationToken = default)
    {
        var contenedor = await cosmos.ObtenerContenedorAsync(cancellationToken);

        var documento = new ValoracionDocumento
        {
            Id = Guid.NewGuid().ToString(),
            ProductoId = valoracion.ProductoId,
            Usuario = valoracion.Usuario,
            Puntaje = valoracion.Puntaje,
            Comentario = valoracion.Comentario,
            FechaUtc = tiempo.GetUtcNow().UtcDateTime
        };

        // La clave de partición viaja con la operación: Cosmos sabe a qué partición ir sin buscar
        await contenedor.CreateItemAsync(documento, new PartitionKey(documento.ProductoId), cancellationToken: cancellationToken);

        return Guid.Parse(documento.Id);
    }
}
