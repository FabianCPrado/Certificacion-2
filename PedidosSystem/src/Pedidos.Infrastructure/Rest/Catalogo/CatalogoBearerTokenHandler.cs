using System.Net.Http.Headers;
using Pedidos.Application.Interfaces.Catalogo;

namespace Pedidos.Infrastructure.Rest.Catalogo;

public sealed class CatalogoBearerTokenHandler(IAuthenticationTokenProvider tokenProvider) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = await tokenProvider.GetAccessTokenAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, cancellationToken);
    }
}