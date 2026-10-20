using Authentication.Domain.Clients;

namespace Authentication.Application.Clients;

public interface IClientSecretHasher
{
    string Hash(ServiceClient client, string secret);

    bool Verify(ServiceClient client, string secret);
}