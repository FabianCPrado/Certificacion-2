using Authentication.Application.Clients;
using Authentication.Domain.Clients;
using Microsoft.EntityFrameworkCore;

namespace Authentication.Infrastructure.Persistence;

public sealed class ServiceClientRepository(AuthenticationDbContext db) : IServiceClientRepository
{
    public Task<ServiceClient?> FindAsync(string clientId, CancellationToken cancellationToken) =>
        db.ServiceClients.AsNoTracking().SingleOrDefaultAsync(c => c.ClientId == clientId, cancellationToken);
}