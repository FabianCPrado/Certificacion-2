using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pedidos.Domain.Pedidos;

namespace Pedidos.Infrastructure.Persistence.MapeoEntidades.Pedidos;

public sealed class LineaPedidoMapeo : IEntityTypeConfiguration<LineaPedido>
{
    public void Configure(EntityTypeBuilder<LineaPedido> builder)
    {
        builder.ToTable("LineasPedido");

        builder.HasKey(l => l.Id);
        // El Id lo genera el dominio: así EF detecta las líneas nuevas como Added
        builder.Property(l => l.Id).ValueGeneratedNever();

        builder.Property(l => l.NombreProducto).HasMaxLength(200);

        builder.OwnsOne(l => l.PrecioUnitario, precio =>
        {
            precio.Property(d => d.Monto).HasColumnName("PrecioUnitario").HasPrecision(18, 2);
            precio.Property(d => d.Moneda).HasColumnName("Moneda").HasMaxLength(3).IsFixedLength();
        });
    }
}
