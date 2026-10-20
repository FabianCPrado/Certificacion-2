using System.ComponentModel.DataAnnotations;
using Authentication.Application.Tokens;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Authentication.Api.Endpoints;

public static class TokenEndpoints
{
    /// <summary>Credenciales de un cliente de servicio registrado.</summary>
    public sealed record IssueTokenRequest
    {
        [Required, StringLength(100, MinimumLength = 1)]
        public required string ClientId { get; init; }

        [Required, StringLength(256, MinimumLength = 32)]
        public required string ClientSecret { get; init; }
    }

    /// <summary>Token de acceso emitido para un cliente de servicio.</summary>
    public sealed record TokenResponse(string AccessToken, string TokenType, DateTimeOffset ExpiresAt);

    public static IEndpointRouteBuilder MapTokenEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/tokens", IssueAsync)
            .WithName("IssueServiceToken")
            .WithSummary("Emite un JWT para un cliente de servicio autenticado")
            .WithDescription("El secreto del cliente se compara con su hash persistido; nunca se almacena ni se devuelve el token.")
            .Produces<TokenResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);

        return app;
    }

    private static async Task<Results<Ok<TokenResponse>, UnauthorizedHttpResult>> IssueAsync(
        IssueTokenRequest request,
        IIssueClientToken issueClientToken,
        CancellationToken cancellationToken)
    {
        var issued = await issueClientToken.ExecuteAsync(request.ClientId, request.ClientSecret, cancellationToken);
        return issued is null
            ? TypedResults.Unauthorized()
            : TypedResults.Ok(new TokenResponse(issued.Value, "Bearer", issued.ExpiresAt));
    }
}