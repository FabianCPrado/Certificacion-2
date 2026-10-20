using Authentication.Application.Clients;
using Authentication.Application.Tokens;
using Authentication.Infrastructure.Persistence;
using Authentication.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Authentication.Infrastructure;

public static class InfrastructureServiceExtensions
{
    private const string DefaultConnection =
        "Server=(localdb)\\MSSQLLocalDB;Database=AuthenticationDB;Trusted_Connection=True;TrustServerCertificate=True";

    public static IServiceCollection AddAuthenticationInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connection = configuration.GetConnectionString("AuthenticationDb") ?? DefaultConnection;
        services.AddDbContext<AuthenticationDbContext>(options => options.UseSqlServer(connection));
        services.AddScoped<IServiceClientRepository, ServiceClientRepository>();
        services.AddScoped<IClientSecretHasher, ClientSecretHasher>();
        services.AddScoped<IIssueClientToken, IssueClientToken>();
        services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();
        return services;
    }
}