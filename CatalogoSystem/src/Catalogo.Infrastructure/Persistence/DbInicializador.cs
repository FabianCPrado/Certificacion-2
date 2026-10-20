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
            await db.Database.ExecuteSqlRawAsync("""
                IF OBJECT_ID(N'dbo.ProductoAtributos', N'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.ProductoAtributos
                    (
                        ProductoId nvarchar(36) NOT NULL,
                        Parametro nvarchar(200) NOT NULL,
                        Valor nvarchar(max) NOT NULL,
                        CONSTRAINT PK_ProductoAtributos PRIMARY KEY (ProductoId, Parametro),
                        CONSTRAINT FK_ProductoAtributos_Productos FOREIGN KEY (ProductoId)
                            REFERENCES dbo.Productos (Id) ON DELETE CASCADE
                    );
                END;

                IF COL_LENGTH(N'dbo.Productos', N'Atributos') IS NOT NULL
                BEGIN
                    EXEC(N'
                        INSERT INTO dbo.ProductoAtributos (ProductoId, Parametro, Valor)
                        SELECT p.Id, a.[key] COLLATE DATABASE_DEFAULT, a.[value]
                        FROM dbo.Productos AS p
                        CROSS APPLY OPENJSON(p.Atributos) AS a
                        WHERE NOT EXISTS
                        (
                            SELECT 1
                            FROM dbo.ProductoAtributos AS existente
                            WHERE existente.ProductoId = p.Id
                              AND existente.Parametro = a.[key] COLLATE DATABASE_DEFAULT
                        );
                    ');
                    ALTER TABLE dbo.Productos DROP COLUMN Atributos;
                END;
                """);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "No se pudo inicializar CatalogoDb (¿está corriendo SQL Server / LocalDB?). Los endpoints del catálogo no van a responder.");
        }
    }
}
