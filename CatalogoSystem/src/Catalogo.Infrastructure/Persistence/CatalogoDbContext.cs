using System.Text.Json;
using Catalogo.Domain.Productos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Catalogo.Infrastructure.Persistence;

/// <summary>Contexto de EF Core para la base CatalogoDb (SQL Server).</summary>
public sealed class CatalogoDbContext(DbContextOptions<CatalogoDbContext> options) : DbContext(options)
{
    public DbSet<ProductoCatalogo> Productos => Set<ProductoCatalogo>();

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

        // Los atributos varían por categoría: se guardan como JSON en una sola columna
        var comparador = new ValueComparer<Dictionary<string, string>>(
            (a, b) => JsonSerializer.Serialize(a, (JsonSerializerOptions?)null) == JsonSerializer.Serialize(b, (JsonSerializerOptions?)null),
            d => d.Aggregate(0, (h, kv) => HashCode.Combine(h, kv.Key, kv.Value)),
            d => new Dictionary<string, string>(d));

        producto.Property(p => p.Atributos)
            .HasConversion(
                d => JsonSerializer.Serialize(d, (JsonSerializerOptions?)null),
                s => JsonSerializer.Deserialize<Dictionary<string, string>>(s, (JsonSerializerOptions?)null) ?? new())
            .HasColumnType("nvarchar(max)")
            .Metadata.SetValueComparer(comparador);
    }
}
