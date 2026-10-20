using Authentication.Application.Clients;

namespace Authentication.Application.Tokens;

public sealed class IssueClientToken(
    IServiceClientRepository clients,
    IClientSecretHasher secretHasher,
    IAccessTokenIssuer tokenIssuer) : IIssueClientToken
{
    public async Task<IssuedAccessToken?> ExecuteAsync(
        string clientId,
        string clientSecret,
        CancellationToken cancellationToken)
    {
        var client = await clients.FindAsync(clientId, cancellationToken);
        if (client is null || !client.IsActive || !secretHasher.Verify(client, clientSecret))
            return null;

        return tokenIssuer.Issue(client);
    }
}