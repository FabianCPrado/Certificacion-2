namespace Pedidos.Application.Interfaces.Catalogo;

public interface IAuthenticationTokenProvider
{
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken);
}