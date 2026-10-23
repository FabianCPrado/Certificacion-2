using Asistente.Infrastructure.Servicios;

namespace Asistente.Tests.Infrastructure.Fakes;

internal sealed class TokenFalso : ITokenProvider
{
    public Task<TokenInfo> ObtenerAsync(bool renovar = false, CancellationToken cancellationToken = default) =>
        Task.FromResult(new TokenInfo("jwt-de-prueba", DateTimeOffset.UtcNow.AddHours(1)));
}
