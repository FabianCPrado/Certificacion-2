using Authentication.Application.Tokens;
using Authentication.Domain.Clients;

namespace Authentication.Application.Tokens;

public interface IAccessTokenIssuer
{
    IssuedAccessToken Issue(ServiceClient client);
}