using Authentication.Domain.Clients;
using Microsoft.EntityFrameworkCore;

namespace Authentication.Infrastructure.Persistence;

public sealed class AuthenticationDbContext(DbContextOptions<AuthenticationDbContext> options) : DbContext(options)
{
    public DbSet<ServiceClient> ServiceClients => Set<ServiceClient>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var client = modelBuilder.Entity<ServiceClient>();
        client.ToTable("ServiceClients");
        client.HasKey(c => c.ClientId);
        client.Property(c => c.ClientId).HasMaxLength(100);
        client.Property(c => c.Name).HasMaxLength(200).IsRequired();
        client.Property(c => c.SecretHash).HasMaxLength(1000).IsRequired();
        client.Property(c => c.IsActive).IsRequired();
        client.Property(c => c.CreatedAt).IsRequired();
    }
}