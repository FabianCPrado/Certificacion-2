using Microsoft.EntityFrameworkCore;
using Pedidos.Domain.Catalogo;

namespace Pedidos.Infrastructure.Cosmos;

/// <summary>
/// DbContext del proveedor de EF Core para Cosmos DB. La configuración va aquí y no en una clase IEntityTypeConfiguration:
/// el AppDbContext (SQL Server) carga todas las configuraciones del ensamblado con ApplyConfigurationsFromAssembly
/// y tomaría también esta, que es de Cosmos.
/// </summary>
public sealed class CatalogoDbContext(DbContextOptions<CatalogoDbContext> options) : DbContext(options)
{
    public DbSet<ProductoCatalogo> Productos => Set<ProductoCatalogo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.Entity<ProductoCatalogo>(b =>
        {
            // Contenedor propio, separado del de la demo con el SDK: EF Core guarda los documentos con su
            // propio formato (nombres de propiedades y un campo de tipo, el discriminador) y así se pueden comparar
            b.ToContainer("ProductosEf");
            b.HasPartitionKey(p => p.Categoria);
            b.HasKey(p => p.Id);
        });
}
