namespace Authentication.Application.Tokens;

public interface IIssueClientToken
{
    Task<IssuedAccessToken?> ExecuteAsync(string clientId, string clientSecret, CancellationToken cancellationToken);
}