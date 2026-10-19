using Clientes.Application.Interfaces.Common;
using Clientes.Domain.Clientes;
using Microsoft.EntityFrameworkCore;

namespace Clientes.Infrastructure.Persistence;

/// <summary>
/// El DbContext ya es una unidad de trabajo (change tracker + SaveChanges transaccional),
/// así que implementa directamente <see cref="IAppDbContext"/>, sin una clase envoltorio.
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options), IAppDbContext
{
    public DbSet<Cliente> Clientes => Set<Cliente>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
