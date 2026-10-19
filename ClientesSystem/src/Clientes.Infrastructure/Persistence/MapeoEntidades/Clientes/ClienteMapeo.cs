using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Clientes.Domain.Clientes;

namespace Clientes.Infrastructure.Persistence.MapeoEntidades.Clientes;

public sealed class ClienteMapeo : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("Clientes");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Nombre).HasMaxLength(200);
        builder.Property(c => c.Email).HasMaxLength(256);
    }
}
