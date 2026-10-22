using Asistente.Application.Asistente;
using Asistente.Domain;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Asistente.Api.Endpoints;

public static class AsistenteEndpoints
{
    public static IEndpointRouteBuilder MapAsistente(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/asistente").WithTags("Asistente");

        g.MapPost("/preguntar", PreguntarAsync)
            .WithSummary("Responde una pregunta en lenguaje natural consultando a los otros servicios");
        g.MapPost("/proponer-pedido", ProponerPedidoAsync)
            .WithSummary("Arma una propuesta de pedido (no crea nada: la confirma una persona)");
        g.MapGet("/herramientas", ListarHerramientasAsync)
            .WithSummary("Lista las herramientas que conoce el asistente (el mapa del sistema)");

        g.MapGet("/salud", SaludAsync)
            .WithSummary("Comprueba si el asistente está conectado a Authentication, Catálogo, Pedidos, Clientes y al modelo");

        return app;
    }

    // 200 si responden todas las conexiones; 503 si alguna falla (el cuerpo siempre detalla cada componente)
    private static async Task<Results<Ok<SaludDto>, JsonHttpResult<SaludDto>>> SaludAsync(ISender sender, CancellationToken ct)
    {
        var salud = await sender.Send(new VerificarSaludQuery(), ct);
        return salud.Estado == "Sano"
            ? TypedResults.Ok(salud)
            : TypedResults.Json(salud, statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    private static async Task<Ok<RespuestaDto>> PreguntarAsync(PreguntaRequest request, ISender sender, CancellationToken ct) =>
        TypedResults.Ok(await sender.Send(new PreguntarQuery(request.Pregunta, request.ClienteId), ct));

    private static async Task<Ok<PropuestaPedidoDto>> ProponerPedidoAsync(PropuestaRequest request, ISender sender, CancellationToken ct) =>
        TypedResults.Ok(await sender.Send(new ProponerPedidoQuery(request.ClienteId, request.Pedido), ct));

    private static async Task<Ok<IReadOnlyList<HerramientaInfo>>> ListarHerramientasAsync(ISender sender, CancellationToken ct) =>
        TypedResults.Ok(await sender.Send(new ListarHerramientasQuery(), ct));
}
