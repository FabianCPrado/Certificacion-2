using Azure.Storage.Blobs;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pedidos.Application.Interfaces.Auditoria;
using Pedidos.Application.Interfaces.Catalogo;
using Pedidos.Application.Interfaces.Clientes;
using Pedidos.Application.Interfaces.Common;
using Pedidos.Application.Interfaces.Demo;
using Pedidos.Application.Interfaces.Pedidos;
using Pedidos.Application.Interfaces.Productos;
using Pedidos.Application.Interfaces.Storage;
using Pedidos.Application.Interfaces.Valoraciones;
using Pedidos.Infrastructure.Caching;
using Pedidos.Infrastructure.Cosmos;
using Pedidos.Infrastructure.Demo;
using Pedidos.Infrastructure.Mongo;
using Pedidos.Infrastructure.Persistence;
using Pedidos.Infrastructure.Persistence.Repositories.Auditoria;
using Pedidos.Infrastructure.Persistence.Repositories.Pedidos;
using Pedidos.Infrastructure.Persistence.Repositories.Productos;
using Pedidos.Infrastructure.Queries.Auditoria;
using Pedidos.Infrastructure.Queries.Pedidos;
using Pedidos.Infrastructure.Queries.Productos;
using Pedidos.Infrastructure.Rest.Clientes;
using Pedidos.Infrastructure.Storage;

namespace Pedidos.Infrastructure;

/// <summary>Conecta cada interfaz de Application con su implementación de infraestructura.</summary>
public static class InfrastructureServiceExtensions
{
    // Clave pública y conocida del emulador de Cosmos DB: no es un secreto, solo sirve contra el emulador local
    private const string CosmosPorDefecto =
        "AccountEndpoint=https://localhost:8083/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Pedidos")
            ?? throw new InvalidOperationException("Falta la cadena de conexión 'Pedidos' (ver README).");

        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

        // Cadena de conexión para el código sin EF Core (Dapper)
        services.AddSingleton(new ConexionSql(connectionString));

        // Interfaz                    → Implementación
        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

        // Escritura (reglas de negocio + persistencia), una por entidad
        services.AddScoped<IPedidoRepository, PedidoRepository>();
        services.AddScoped<IProductoRepository, ProductoRepository>();
        services.AddSingleton<IAuditoriaRepository, AuditoriaMongoRepository>();   // auditoría en MongoDB

        // Lectura (proyecciones y SQL directo), una por entidad
        services.AddScoped<IPedidosConsultas, PedidosConsultas>();
        // Reporte con Dapper puro (procedimiento almacenado y SELECT + LINQ a objetos), sin EF Core
        services.AddScoped<IPedidosReportes, PedidosReportes>();
        services.AddScoped<IProductoConsultas, ProductosConsultas>();
        services.AddSingleton<IAuditoriaConsultas, AuditoriaMongoConsultas>();

        // Clientes vive en su propio servicio (ClientesSystem), que es el único que hace el CRUD.
        // Pedidos solo lo consulta (GET) al momento por REST: Application usa IClienteConsultas y no sabe que hay HTTP detrás.
        var clientesUrl = configuration["Servicios:Clientes"]
            ?? throw new InvalidOperationException("Falta la URL del servicio Clientes 'Servicios:Clientes' (ver README).");
        services.AddHttpClient<IClienteConsultas, ClientesRestConsultas>(http =>
        {
            http.BaseAddress = new Uri(clientesUrl);
            // Tiempo máximo de respuesta del servicio Clientes: 30 s (pasado ese tiempo la API responde 503)
            http.Timeout = TimeSpan.FromSeconds(30);
        });

        // Solo Clase 4: versiones "malas" para comparar
        services.AddScoped<IDemoMalasPracticas, DemoMalasPracticas>();

        // MongoDB (auditoría): un solo cliente compartido, con su propio pool de conexiones
        var mongoConexion = configuration.GetConnectionString("Mongo") ?? "mongodb://localhost:27017/PedidosDb";
        services.AddSingleton(provider => new MongoAuditoriaColeccion(
            mongoConexion, provider.GetRequiredService<ILogger<MongoAuditoriaColeccion>>()));

        // Cosmos DB (emulador local en Docker por defecto). El CosmosClient es Singleton: uno solo para toda la aplicación
        var cosmosConexion = configuration.GetConnectionString("Cosmos") ?? CosmosPorDefecto;
        services.AddSingleton(_ => CosmosOpciones.CrearCliente(cosmosConexion));
        services.AddSingleton<CosmosValoraciones>();
        services.AddSingleton<IValoracionRepository, ValoracionCosmosRepository>();
        services.AddSingleton<IValoracionConsultas, ValoracionCosmosConsultas>();
        services.AddSingleton<ICatalogoRepository, CatalogoRepository>();   // catálogo con el SDK

        // Catálogo con el proveedor de EF Core para Cosmos (Scoped, como cualquier DbContext)
        services.AddDbContext<CatalogoDbContext>(options =>
            options.UseCosmos(cosmosConexion, CosmosOpciones.BaseDatos, cosmos => cosmos
                .ConnectionMode(ConnectionMode.Gateway)
                .HttpClientFactory(CosmosOpciones.CrearHttpClient)));

        // Blob Storage (Azurite en local): las imágenes de los productos
        services.AddSingleton(new BlobServiceClient(configuration.GetConnectionString("Storage") ?? "UseDevelopmentStorage=true"));
        services.AddSingleton<IImagenesStorage, BlobImagenesStorage>();

        // Solo Clase 5: demos de Redis, Cosmos con SDK y Cosmos con EF Core
        services.AddScoped<IDemoCache, DemoCache>();
        services.AddSingleton<IDemoCatalogo, DemoCatalogo>();
        services.AddScoped<IDemoCatalogoEf, DemoCatalogoEf>();

        // Caché con Redis (demo): Application usa ICacheService y no sabe que detrás hay Redis
        // abortConnect=false y timeouts cortos: si Redis está caído la app no se queda esperando ni falla al arrancar
        services.AddStackExchangeRedisCache(options =>
            options.Configuration = configuration.GetConnectionString("Redis")
                ?? "localhost:6379,abortConnect=false,connectTimeout=1000,syncTimeout=1000,asyncTimeout=1000");
        services.AddSingleton<ICacheService, RedisCacheService>();

        services.AddSingleton(TimeProvider.System);

        return services;
    }
}
