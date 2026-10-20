using Catalogo.Domain.Productos;
using Microsoft.EntityFrameworkCore;

namespace Catalogo.Infrastructure.Persistence;

/// <summary>Contexto de EF Core para la base CatalogoDb (SQL Server).</summary>
public sealed class CatalogoDbContext(DbContextOptions<CatalogoDbContext> options) : DbContext(options)
{
    public DbSet<ProductoCatalogo> Productos => Set<ProductoCatalogo>();

    public DbSet<ProductoAtributo> AtributosProducto => Set<ProductoAtributo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var producto = modelBuilder.Entity<ProductoCatalogo>();

        producto.ToTable("Productos");
        producto.HasKey(p => p.Id);
        producto.Property(p => p.Id).HasMaxLength(36);
        producto.Property(p => p.Categoria).HasMaxLength(100).IsRequired();
        producto.Property(p => p.Nombre).HasMaxLength(200).IsRequired();
        producto.Property(p => p.Precio).HasPrecision(18, 2);
        producto.HasIndex(p => new { p.Categoria, p.Nombre });

        producto.HasMany(p => p.Atributos)
            .WithOne()
            .HasForeignKey(a => a.ProductoId)
            .OnDelete(DeleteBehavior.Cascade);

        var atributo = modelBuilder.Entity<ProductoAtributo>();
        atributo.ToTable("ProductoAtributos");
        atributo.HasKey(a => new { a.ProductoId, a.Parametro });
        atributo.Property(a => a.ProductoId).HasMaxLength(36);
        atributo.Property(a => a.Parametro).HasMaxLength(200).IsRequired();
        atributo.Property(a => a.Valor).IsRequired();
    }
}
