using Catalogo.Application.Interfaces;
using Catalogo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Catalogo.Infrastructure;

/// <summary>Conecta cada interfaz de Application con su implementación de infraestructura.</summary>
public static class InfrastructureServiceExtensions
{
    private const string ConexionPorDefecto =
        "Server=(localdb)\\MSSQLLocalDB;Database=CatalogoDb;Trusted_Connection=True;TrustServerCertificate=True";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var conexion = configuration.GetConnectionString("CatalogoDb") ?? ConexionPorDefecto;
        services.AddDbContext<CatalogoDbContext>(o => o.UseSqlServer(conexion));
        services.AddScoped<ICatalogoRepository, CatalogoRepository>();

        return services;
    }
}
