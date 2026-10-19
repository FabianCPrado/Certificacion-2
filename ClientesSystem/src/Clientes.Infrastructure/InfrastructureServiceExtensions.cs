using Clientes.Application.Interfaces.Clientes;
using Clientes.Application.Interfaces.Common;
using Clientes.Application.Interfaces.Pedidos;
using Clientes.Infrastructure.Persistence;
using Clientes.Infrastructure.Persistence.Repositories.Clientes;
using Clientes.Infrastructure.Queries.Clientes;
using Clientes.Infrastructure.Rest.Pedidos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Clientes.Infrastructure;

/// <summary>Conecta cada interfaz de Application con su implementación de infraestructura.</summary>
public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Clientes")
            ?? throw new InvalidOperationException("Falta la cadena de conexión 'Clientes' (ver README).");

        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

        // Interfaz                    → Implementación
        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

        // Escritura (reglas de negocio + persistencia)
        services.AddScoped<IClienteRepository, ClienteRepository>();

        // Lectura (proyecciones directas al DTO)
        services.AddScoped<IClienteConsultas, ClientesConsultas>();

        // Pedidos vive en su propio servicio: solo se le consulta (GET) por REST, para la regla de eliminar clientes
        var pedidosUrl = configuration["Servicios:Pedidos"]
            ?? throw new InvalidOperationException("Falta la URL del servicio Pedidos 'Servicios:Pedidos' (ver README).");
        services.AddHttpClient<IPedidosConsultas, PedidosRestConsultas>(http =>
        {
            http.BaseAddress = new Uri(pedidosUrl);
            // Tiempo máximo de respuesta del servicio Pedidos: 30 s (pasado ese tiempo la API responde 503)
            http.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }
}

