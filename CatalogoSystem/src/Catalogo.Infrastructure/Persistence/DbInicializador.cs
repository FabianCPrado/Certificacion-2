using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Catalogo.Infrastructure.Persistence;

public static class DbInicializador
{
    /// <summary>Solo en Development: crea la base CatalogoDb y sus tablas si no existen. Si SQL Server no responde, avisa y deja arrancar.</summary>
    public static async Task InicializarBaseDatosAsync(this IServiceProvider servicios)
    {
        using var scope = servicios.CreateScope();
        var log = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbInicializador");

        try
        {
            var db = scope.ServiceProvider.GetRequiredService<CatalogoDbContext>();
            await db.Database.EnsureCreatedAsync();
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "No se pudo inicializar CatalogoDb (¿está corriendo SQL Server / LocalDB?). Los endpoints del catálogo no van a responder.");
        }
    }
}
