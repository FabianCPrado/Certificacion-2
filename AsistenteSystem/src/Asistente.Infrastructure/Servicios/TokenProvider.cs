using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;

namespace Asistente.Infrastructure.Servicios;

public sealed record TokenInfo(string AccessToken, DateTimeOffset ExpiraEn);

/// <summary>JWT de asistente-service. Se pide la primera vez que alguien lo necesita; nunca al arrancar.</summary>
public interface ITokenProvider
{
    Task<TokenInfo> ObtenerAsync(bool renovar = false, CancellationToken cancellationToken = default);
}

/// <summary>Pide el token a Authentication (POST /api/tokens) y lo guarda en memoria hasta 1 minuto antes de que venza.</summary>
public sealed class TokenProvider(IHttpClientFactory httpClientFactory, IConfiguration configuration, TimeProvider tiempo)
    : ITokenProvider
{
    public const string NombreCliente = "Authentication";
    private static readonly TimeSpan Margen = TimeSpan.FromMinutes(1);

    private readonly SemaphoreSlim _candado = new(1, 1);
    private TokenInfo? _token;

    public async Task<TokenInfo> ObtenerAsync(bool renovar = false, CancellationToken cancellationToken = default)
    {
        if (!renovar && Vigente(_token) is { } actual)
            return actual;

        await _candado.WaitAsync(cancellationToken);
        try
        {
            if (!renovar && Vigente(_token) is { } vigente)
                return vigente;

            _token = await PedirAsync(cancellationToken);
            return _token;
        }
        finally
        {
            _candado.Release();
        }
    }

    private TokenInfo? Vigente(TokenInfo? token) =>
        token is not null && token.ExpiraEn - Margen > tiempo.GetUtcNow() ? token : null;

    private async Task<TokenInfo> PedirAsync(CancellationToken cancellationToken)
    {
        var clientId = configuration["Authentication:ClientId"];
        var clientSecret = configuration["Authentication:ClientSecret"];
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
            throw new InvalidOperationException("Falta Authentication:ClientSecret: configúrelo con User Secrets.");

        var http = httpClientFactory.CreateClient(NombreCliente);
        using var respuesta = await http.PostAsJsonAsync("/api/tokens", new { clientId, clientSecret }, cancellationToken);

        if (!respuesta.IsSuccessStatusCode)
            throw new HttpRequestException($"Authentication rechazó al cliente {clientId} (HTTP {(int)respuesta.StatusCode}).");

        var token = await respuesta.Content.ReadFromJsonAsync<TokenRespuesta>(cancellationToken)
            ?? throw new HttpRequestException("Authentication devolvió una respuesta vacía.");

        return new TokenInfo(token.AccessToken, token.ExpiresAt);
    }

    private sealed record TokenRespuesta(string AccessToken, string TokenType, DateTimeOffset ExpiresAt);
}
