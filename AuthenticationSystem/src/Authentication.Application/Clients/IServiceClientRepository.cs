using Authentication.Domain.Clients;

namespace Authentication.Application.Clients;

public interface IServiceClientRepository
{
    Task<ServiceClient?> FindAsync(string clientId, CancellationToken cancellationToken);
}