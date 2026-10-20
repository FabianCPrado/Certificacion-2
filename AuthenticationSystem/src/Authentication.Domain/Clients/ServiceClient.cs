namespace Authentication.Domain.Clients;

public sealed class ServiceClient
{
    private ServiceClient() { }

    public ServiceClient(string clientId, string name, string secretHash)
    {
        ClientId = clientId;
        Name = name;
        SecretHash = secretHash;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public string ClientId { get; private set; } = "";

    public string Name { get; private set; } = "";

    public string SecretHash { get; private set; } = "";

    public bool IsActive { get; private set; } = true;

    public DateTimeOffset CreatedAt { get; private set; }

    public void ReplaceSecretHash(string secretHash) => SecretHash = secretHash;

    public void Deactivate() => IsActive = false;
}