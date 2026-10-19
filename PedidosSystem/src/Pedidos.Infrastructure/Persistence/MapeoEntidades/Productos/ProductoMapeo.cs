using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pedidos.Domain.Productos;

namespace Pedidos.Infrastructure.Persistence.MapeoEntidades.Productos;

public sealed class ProductoMapeo : IEntityTypeConfiguration<Producto>
{
    public void Configure(EntityTypeBuilder<Producto> builder)
    {
        builder.ToTable("Productos");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Nombre).HasMaxLength(200);
        builder.Property(p => p.Categoria).HasMaxLength(50);
        builder.Property(p => p.Precio).HasPrecision(18, 2);

        builder.HasIndex(p => p.Categoria);
    }
}
