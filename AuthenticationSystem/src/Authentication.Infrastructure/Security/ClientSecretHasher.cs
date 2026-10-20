using Authentication.Application.Clients;
using Authentication.Domain.Clients;
using Microsoft.AspNetCore.Identity;

namespace Authentication.Infrastructure.Security;

public sealed class ClientSecretHasher : IClientSecretHasher
{
    private readonly PasswordHasher<ServiceClient> _hasher = new();

    public string Hash(ServiceClient client, string secret) => _hasher.HashPassword(client, secret);

    public bool Verify(ServiceClient client, string secret) =>
        _hasher.VerifyHashedPassword(client, client.SecretHash, secret) != PasswordVerificationResult.Failed;
}