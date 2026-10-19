namespace Pedidos.Application.Interfaces.Common;

/// <summary>
/// Contexto de persistencia de Pedidos: confirma en una sola transacción todos los cambios.
/// La define Application (lo que necesita) y la implementa <c>AppDbContext</c> en Infrastructure.
/// No expone DbSet para que Application no dependa de EF Core; el acceso a datos va por los repositorios.
/// </summary>
public interface IAppDbContext
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
