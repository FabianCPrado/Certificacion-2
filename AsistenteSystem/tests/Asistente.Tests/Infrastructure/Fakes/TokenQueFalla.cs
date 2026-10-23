using Asistente.Infrastructure.Servicios;

namespace Asistente.Tests.Infrastructure.Fakes;

internal sealed class TokenQueFalla : ITokenProvider
{
    public Task<TokenInfo> ObtenerAsync(bool renovar = false, CancellationToken cancellationToken = default) =>
        throw new HttpRequestException("Authentication no responde.");
}
