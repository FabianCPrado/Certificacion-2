using Microsoft.Azure.Cosmos;

namespace Pedidos.Infrastructure.Cosmos;

/// <summary>
/// Acceso al contenedor "valoraciones" de Cosmos DB. Usa el <see cref="CosmosClient"/> Singleton compartido.
/// El contenedor se crea la primera vez que se usa.
/// </summary>
public sealed class CosmosValoraciones(CosmosClient cliente) : IDisposable
{
    private const string NombreContenedor = "valoraciones";
    private const string ClaveParticion = "/productoId";

    private readonly SemaphoreSlim _candado = new(1, 1);
    private Container? _contenedor;

    public async Task<Container> ObtenerContenedorAsync(CancellationToken cancellationToken)
    {
        if (_contenedor is not null)
            return _contenedor;

        await _candado.WaitAsync(cancellationToken);
        try
        {
            if (_contenedor is null)
            {
                var baseDatos = await cliente.CreateDatabaseIfNotExistsAsync(CosmosOpciones.BaseDatos, cancellationToken: cancellationToken);
                var respuesta = await baseDatos.Database.CreateContainerIfNotExistsAsync(
                    new ContainerProperties(NombreContenedor, ClaveParticion), cancellationToken: cancellationToken);
                _contenedor = respuesta.Container;
            }

            return _contenedor;
        }
        finally
        {
            _candado.Release();
        }
    }

    public void Dispose() => _candado.Dispose();
}
