using System.Net;
using Microsoft.Azure.Cosmos;
using Pedidos.Application.Interfaces.Catalogo;
using Pedidos.Domain.Catalogo;

namespace Pedidos.Infrastructure.Cosmos;

/// <summary>Catálogo de productos en Cosmos DB con el SDK. Usa el <see cref="CosmosClient"/> Singleton y no guarda estado propio.</summary>
public sealed class CatalogoRepository(CosmosClient cosmos) : ICatalogoRepository
{
    private Container Contenedor => cosmos.GetContainer(CosmosOpciones.BaseDatos, CosmosOpciones.ContenedorCatalogo);

    public async Task<ProductoCatalogo?> ObtenerAsync(string id, string categoria, CancellationToken cancellationToken = default)
    {
        try
        {
            var respuesta = await Contenedor.ReadItemAsync<ProductoCatalogo>(id, new PartitionKey(categoria), cancellationToken: cancellationToken);
            return respuesta.Resource;   // point read: lo más barato
        }
        catch (CosmosException e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<ProductoCatalogo>> PorCategoriaAsync(string categoria, CancellationToken cancellationToken = default)
    {
        var consulta = new QueryDefinition("SELECT * FROM c WHERE c.categoria = @categoria")
            .WithParameter("@categoria", categoria);   // parámetros, como en la Clase 4

        using var iterador = Contenedor.GetItemQueryIterator<ProductoCatalogo>(
            consulta, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(categoria) });

        var items = new List<ProductoCatalogo>();
        while (iterador.HasMoreResults)
            items.AddRange(await iterador.ReadNextAsync(cancellationToken));

        return items;
    }

    public Task GuardarAsync(ProductoCatalogo producto, CancellationToken cancellationToken = default) =>
        Contenedor.UpsertItemAsync(producto, new PartitionKey(producto.Categoria), cancellationToken: cancellationToken);
}
