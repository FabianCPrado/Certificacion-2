using Asistente.Application.Asistente;
using Asistente.Application.Sistemas;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Asistente.Api.Endpoints;

/// <summary>Apartado Sistemas: un único endpoint donde se le pregunta a la IA sobre el ecosistema.</summary>
public static class SistemasEndpoints
{
    public static IEndpointRouteBuilder MapSistemas(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/sistemas").WithTags("Sistemas");

        g.MapPost("/preguntar", PreguntarAsync)
            .WithSummary("Preguntale a la IA todo lo que necesites saber del ecosistema de sistemas")
            .WithDescription(
                "Responde con la estructura de cada sistema (capas, endpoints, puertos, bases de datos), las dependencias " +
                "entre sistemas, qué se rompe si cambiás algo, la arquitectura y las guías paso a paso. " +
                "Ejemplos: ¿Qué sistemas hay y cómo se hablan?, ¿Qué se rompe si cambio el DTO de productos de Catálogo?, " +
                "¿Cómo agrego un endpoint nuevo en Pedidos?, ¿Cómo se manejan los errores? " +
                "En 'fuentes' indica de qué sistema o documento sale la respuesta y en 'uso' lo que consumió. " +
                "No consulta datos de negocio (productos, pedidos, clientes): para eso está /api/asistente/preguntar.");

        return app;
    }

    private static async Task<Ok<RespuestaDto>> PreguntarAsync(PreguntaSistemasRequest request, ISender sender, CancellationToken ct) =>
        TypedResults.Ok(await sender.Send(new PreguntarSobreSistemasQuery(request.Pregunta), ct));
}
