using System.Net.Http.Headers;

namespace Asistente.Infrastructure.Servicios;

/// <summary>Agrega el JWT de asistente-service a cada llamada saliente.</summary>
public sealed class BearerTokenHandler(ITokenProvider tokens) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await tokens.ObtenerAsync(cancellationToken: cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        return await base.SendAsync(request, cancellationToken);
    }
}
