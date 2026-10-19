using System.Net;
using Microsoft.Azure.Cosmos;
using Pedidos.Application.Interfaces.Demo;
using Pedidos.Domain.Catalogo;

namespace Pedidos.Infrastructure.Cosmos;

/// <summary>
/// Demo de la Clase 5 con el SDK de Cosmos DB. Cada consulta devuelve el costo en RU para poder compararlas.
/// OJO: <see cref="ConsultarMaloAsync"/> arma el texto de la consulta concatenando a propósito, para mostrar la inyección.
/// </summary>
public sealed class DemoCatalogo(CosmosClient cosmos) : IDemoCatalogo
{
    private const int TotalProductos = 300;

    private static readonly string[] Categorias = ["Bebidas", "Electro", "Indumentaria"];

    private Container Contenedor => cosmos.GetContainer(CosmosOpciones.BaseDatos, CosmosOpciones.ContenedorCatalogo);

    public async Task<int> CargarAsync(CancellationToken cancellationToken = default)
    {
        for (var i = 1; i <= TotalProductos; i++)
        {
            var categoria = Categorias[i % Categorias.Length];
            var producto = new ProductoCatalogo
            {
                Id = $"prod-{i}",   // id fijo: repetir la carga reemplaza los mismos documentos en vez de duplicarlos
                Categoria = categoria,
                Nombre = $"{categoria} {i}",
                Precio = 1000 + i,
                Atributos = categoria switch
                {
                    "Bebidas" => new() { ["litros"] = "1.5" },
                    "Electro" => new() { ["pulgadas"] = "55", ["voltaje"] = "220V" },
                    _ => new() { ["talle"] = "M", ["color"] = "Negro" }
                }
            };

            await Contenedor.UpsertItemAsync(producto, new PartitionKey(producto.Categoria), cancellationToken: cancellationToken);
        }

        return TotalProductos;
    }

    public async Task<LecturaCatalogoDto?> LeerPuntualAsync(string categoria, string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var respuesta = await Contenedor.ReadItemAsync<ProductoCatalogo>(id, new PartitionKey(categoria), cancellationToken: cancellationToken);
            return new LecturaCatalogoDto(respuesta.RequestCharge, respuesta.Resource);
        }
        catch (CosmosException e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public Task<ConsultaCatalogoDto> ConsultarMaloAsync(string categoria, CancellationToken cancellationToken = default)
    {
        // MALO A PROPÓSITO: el valor entra en el texto de la consulta. Con  ' OR '1'='1  devuelve todas las categorías.
        // Es la versión insegura de la demo de la Clase 5; el analizador lo marca y es lo que se quiere mostrar.
#pragma warning disable S2077
        var iterador = Contenedor.GetItemQueryIterator<ProductoCatalogo>(
            $"SELECT * FROM c WHERE c.categoria = '{categoria}'");
#pragma warning restore S2077

        return LeerTodoAsync(iterador, cancellationToken);
    }

    public Task<ConsultaCatalogoDto> ConsultarBuenoAsync(string categoria, CancellationToken cancellationToken = default)
    {
        var consulta = new QueryDefinition("SELECT * FROM c WHERE c.categoria = @categoria")
            .WithParameter("@categoria", categoria);

        var iterador = Contenedor.GetItemQueryIterator<ProductoCatalogo>(
            consulta, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(categoria) });

        return LeerTodoAsync(iterador, cancellationToken);
    }

    public Task<ConsultaCatalogoDto> ConsultarCruzadaAsync(string nombre, CancellationToken cancellationToken = default)
    {
        var consulta = new QueryDefinition("SELECT * FROM c WHERE c.nombre = @nombre")
            .WithParameter("@nombre", nombre);

        // Sin PartitionKey: la consulta cruza todas las particiones
        return LeerTodoAsync(Contenedor.GetItemQueryIterator<ProductoCatalogo>(consulta), cancellationToken);
    }

    private static async Task<ConsultaCatalogoDto> LeerTodoAsync(
        FeedIterator<ProductoCatalogo> iterador, CancellationToken cancellationToken)
    {
        using (iterador)
        {
            var cantidad = 0;
            double ru = 0;

            while (iterador.HasMoreResults)
            {
                var pagina = await iterador.ReadNextAsync(cancellationToken);
                cantidad += pagina.Count;
                ru += pagina.RequestCharge;
            }

            return new ConsultaCatalogoDto(cantidad, ru);
        }
    }
}
