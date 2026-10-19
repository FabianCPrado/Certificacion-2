using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pedidos.Domain.Auditoria;

namespace Pedidos.Infrastructure.Persistence.MapeoEntidades.Auditoria;

public sealed class RegistroAuditoriaMapeo : IEntityTypeConfiguration<RegistroAuditoria>
{
    public void Configure(EntityTypeBuilder<RegistroAuditoria> builder)
    {
        builder.ToTable("Auditoria");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.Metodo).HasMaxLength(10);
        builder.Property(a => a.Ruta).HasMaxLength(300);
        builder.Property(a => a.Usuario).HasMaxLength(100);
        builder.Property(a => a.TraceId).HasMaxLength(100);

        builder.HasIndex(a => a.FechaUtc);
        builder.HasIndex(a => a.Ruta);
    }
}
