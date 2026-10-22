using Asistente.Application.Interfaces;
using Asistente.Infrastructure.IA;
using Asistente.Infrastructure.Mapa;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Asistente.Infrastructure;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration, string rutaMapa)
    {
        services.AddSingleton<IMapaSistema>(_ => new MapaSistema(rutaMapa));
        services.AddSingleton<IAsistente, AsistenteNoConfigurado>();
        return services;
    }
}
