using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pedidos.Domain.Pedidos;

namespace Pedidos.Infrastructure.Persistence.MapeoEntidades.Pedidos;

public sealed class PedidoMapeo : IEntityTypeConfiguration<Pedido>
{
    public void Configure(EntityTypeBuilder<Pedido> builder)
    {
        builder.ToTable("Pedidos");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Moneda).HasMaxLength(3).IsFixedLength();
        builder.Property(p => p.Estado).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(p => p.ClienteId);

        // Objetos de valor como owned types: se guardan en columnas de la tabla Pedidos
        builder.OwnsOne(p => p.EmailCliente, email =>
            email.Property(e => e.Valor).HasColumnName("EmailCliente").HasMaxLength(256));

        builder.OwnsOne(p => p.DireccionEnvio, direccion =>
        {
            direccion.Property(d => d.Calle).HasColumnName("Calle").HasMaxLength(200);
            direccion.Property(d => d.Ciudad).HasColumnName("Ciudad").HasMaxLength(100);
            direccion.Property(d => d.CodigoPostal).HasColumnName("CodigoPostal").HasMaxLength(20);
            direccion.Property(d => d.Pais).HasColumnName("Pais").HasMaxLength(100);
        });

        builder.HasMany(p => p.Lineas)
            .WithOne()
            .HasForeignKey("PedidoId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
