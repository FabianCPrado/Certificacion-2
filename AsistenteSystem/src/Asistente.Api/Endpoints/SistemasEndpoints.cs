using Asistente.Application.Sistemas;
using Asistente.Domain;
using Asistente.Infrastructure.Mapa;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Asistente.Api.Endpoints;

/// <summary>Apartado Sistemas: la estructura de cada sistema y la base de conocimiento (arquitectura e instrucciones).</summary>
public static class SistemasEndpoints
{
    public static IEndpointRouteBuilder MapSistemas(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/sistemas").WithTags("Sistemas");

        g.MapGet("/", ListarAsync)
            .WithSummary("Lista los sistemas con un resumen: puertos, base de datos, tecnologías y de quién dependen");
        g.MapGet("/buscar", BuscarAsync)
            .WithSummary("Busca un texto en la estructura, la arquitectura y las instrucciones (sin importar tildes)")
            .WithDescription("Sirve para saber dónde está algo antes de cambiarlo. Ejemplos: stock, detalle, JWT, EnsureCreated.");

        g.MapGet("/arquitectura", ListarArquitecturaAsync)
            .WithSummary("Lista los documentos de arquitectura (capas, CQRS, errores, seguridad, datos, pruebas)");
        g.MapGet("/arquitectura/{id}", ObtenerArquitecturaAsync)
            .WithSummary("Un documento de arquitectura completo, en Markdown");

        g.MapGet("/instrucciones", ListarInstruccionesAsync)
            .WithSummary("Lista las guías paso a paso (agregar un endpoint, cambiar un contrato, crear un sistema...)");
        g.MapGet("/instrucciones/{id}", ObtenerInstruccionAsync)
            .WithSummary("Una guía paso a paso completa, en Markdown");

        g.MapGet("/{nombre}", ObtenerAsync)
            .WithSummary("La estructura completa de un sistema: capas, endpoints, dependencias, contratos, tests y notas")
            .WithDescription("El nombre puede ser Pedidos o PedidosSystem. Si no existe, responde 404 con los nombres válidos.");
        g.MapGet("/{nombre}/impacto", ImpactoAsync)
            .WithSummary("Qué sistemas consumen a este y qué contratos usan: qué se rompe si lo cambiás");

        return app;
    }

    private static async Task<Ok<IReadOnlyList<SistemaResumenDto>>> ListarAsync(ISender sender, CancellationToken ct) =>
        TypedResults.Ok(await sender.Send(new ListarSistemasQuery(), ct));

    private static async Task<Ok<BusquedaDto>> BuscarAsync(string? texto, ISender sender, CancellationToken ct) =>
        TypedResults.Ok(await sender.Send(new BuscarConocimientoQuery(texto ?? string.Empty), ct));

    private static async Task<Ok<IReadOnlyList<DocumentoResumenDto>>> ListarArquitecturaAsync(ISender sender, CancellationToken ct) =>
        TypedResults.Ok(await sender.Send(new ListarDocumentosQuery(BibliotecaDocumentos.Arquitectura), ct));

    private static async Task<Ok<DocumentoInfo>> ObtenerArquitecturaAsync(string id, ISender sender, CancellationToken ct) =>
        TypedResults.Ok(await sender.Send(new ObtenerDocumentoQuery(BibliotecaDocumentos.Arquitectura, id), ct));

    private static async Task<Ok<IReadOnlyList<DocumentoResumenDto>>> ListarInstruccionesAsync(ISender sender, CancellationToken ct) =>
        TypedResults.Ok(await sender.Send(new ListarDocumentosQuery(BibliotecaDocumentos.Instrucciones), ct));

    private static async Task<Ok<DocumentoInfo>> ObtenerInstruccionAsync(string id, ISender sender, CancellationToken ct) =>
        TypedResults.Ok(await sender.Send(new ObtenerDocumentoQuery(BibliotecaDocumentos.Instrucciones, id), ct));

    private static async Task<Ok<SistemaInfo>> ObtenerAsync(string nombre, ISender sender, CancellationToken ct) =>
        TypedResults.Ok(await sender.Send(new ObtenerSistemaQuery(nombre), ct));

    private static async Task<Ok<ImpactoDto>> ImpactoAsync(string nombre, ISender sender, CancellationToken ct) =>
        TypedResults.Ok(await sender.Send(new ImpactoDeSistemaQuery(nombre), ct));
}
