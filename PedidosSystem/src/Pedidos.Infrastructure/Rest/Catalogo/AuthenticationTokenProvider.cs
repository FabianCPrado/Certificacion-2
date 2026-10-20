using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Pedidos.Application.Interfaces.Catalogo;

namespace Pedidos.Infrastructure.Rest.Catalogo;

public sealed class AuthenticationTokenProvider(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration) : IAuthenticationTokenProvider
{
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private string? _accessToken;
    private DateTimeOffset _expiresAt;

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (HasUsableToken())
            return _accessToken!;

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            if (HasUsableToken())
                return _accessToken!;

            var clientId = configuration["ClientCredentials:ClientId"] ?? "pedidos-service";
            var clientSecret = configuration["ClientCredentials:PedidosSecret"];
            if (string.IsNullOrWhiteSpace(clientSecret))
                throw new InvalidOperationException("Configure ClientCredentials:PedidosSecret con User Secrets en Pedidos.Api.");

            using var response = await httpClientFactory.CreateClient("authentication")
                .PostAsJsonAsync("api/tokens", new TokenRequest(clientId, clientSecret), cancellationToken);
            response.EnsureSuccessStatusCode();

            var token = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken)
                ?? throw new HttpRequestException("AuthenticationSystem devolvió una respuesta vacía.");
            _accessToken = token.AccessToken;
            _expiresAt = token.ExpiresAt;
            return _accessToken;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private bool HasUsableToken() =>
        !string.IsNullOrWhiteSpace(_accessToken) && _expiresAt > DateTimeOffset.UtcNow.AddSeconds(30);

    private sealed record TokenRequest(
        [property: JsonPropertyName("clientId")] string ClientId,
        [property: JsonPropertyName("clientSecret")] string ClientSecret);

    private sealed record TokenResponse(
        [property: JsonPropertyName("accessToken")] string AccessToken,
        [property: JsonPropertyName("tokenType")] string TokenType,
        [property: JsonPropertyName("expiresAt")] DateTimeOffset ExpiresAt);
}