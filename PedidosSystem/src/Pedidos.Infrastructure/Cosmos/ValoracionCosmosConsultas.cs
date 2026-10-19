using Microsoft.Azure.Cosmos;
using Pedidos.Application.Interfaces.Valoraciones;
using Pedidos.Application.Valoraciones.Queries.ListarValoraciones;

namespace Pedidos.Infrastructure.Cosmos;

/// <summary>Lee valoraciones desde Cosmos DB, siempre dentro de la partición del producto.</summary>
public sealed class ValoracionCosmosConsultas(CosmosValoraciones cosmos) : IValoracionConsultas
{
    public async Task<IReadOnlyList<ValoracionDto>> ListarPorProductoAsync(
        string productoId, CancellationToken cancellationToken = default)
    {
        var contenedor = await cosmos.ObtenerContenedorAsync(cancellationToken);
        var clave = productoId;

        var consulta = new QueryDefinition("SELECT * FROM c WHERE c.productoId = @productoId ORDER BY c.fechaUtc DESC")
            .WithParameter("@productoId", clave);

        // PartitionKey en las opciones: la consulta se resuelve en una sola partición (barata y rápida)
        using var iterador = contenedor.GetItemQueryIterator<ValoracionDocumento>(
            consulta, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(clave) });

        var resultado = new List<ValoracionDto>();
        while (iterador.HasMoreResults)
        {
            var pagina = await iterador.ReadNextAsync(cancellationToken);
            resultado.AddRange(pagina.Select(d => new ValoracionDto(
                Guid.Parse(d.Id), d.ProductoId, d.Usuario, d.Puntaje, d.Comentario, d.FechaUtc)));
        }

        return resultado;
    }
}
