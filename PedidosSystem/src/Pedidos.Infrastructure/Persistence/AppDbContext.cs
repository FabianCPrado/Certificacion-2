using Microsoft.EntityFrameworkCore;
using Pedidos.Application.Interfaces.Common;
using Pedidos.Domain.Auditoria;
using Pedidos.Domain.Pedidos;

namespace Pedidos.Infrastructure.Persistence;

/// <summary>
/// El DbContext ya es una unidad de trabajo (change tracker + SaveChanges transaccional),
/// así que implementa directamente <see cref="IAppDbContext"/>, sin una clase envoltorio.
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options), IAppDbContext
{
    public DbSet<Pedido> Pedidos => Set<Pedido>();

    public DbSet<RegistroAuditoria> Auditoria => Set<RegistroAuditoria>();

    // Solo para mostrar el problema N+1 en la demo: en el código normal las líneas se acceden desde el pedido.
    public DbSet<LineaPedido> LineasPedido => Set<LineaPedido>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
