using System.Globalization;
using Asistente.Application.Exceptions;
using Asistente.Application.Interfaces;
using Asistente.Domain;
using FluentValidation;
using MediatR;

namespace Asistente.Application.Sistemas;

public sealed record SistemaResumenDto(
    string Nombre, string Descripcion, int PuertoHttps, int PuertoHttp, string? BaseDeDatos,
    IReadOnlyList<string> Tecnologias, int CantidadEndpoints, IReadOnlyList<string> Consume);

public sealed record ConsumidorDto(string Sistema, string Para, string Como);

/// <summary>Qué se ve afectado si se cambia un sistema: quién lo consume y qué contratos usan.</summary>
public sealed record ImpactoDto(
    string Sistema, IReadOnlyList<ConsumidorDto> LoConsumen, IReadOnlyList<ContratoInfo> Contratos,
    IReadOnlyList<string> Notas, string Resumen);

public sealed record DocumentoResumenDto(string Id, string Titulo, string Resumen);

/// <summary><c>Tipo</c>: Sistema, Endpoint, Capa, Dependencia, Contrato, Nota, Arquitectura o Instrucciones.</summary>
public sealed record CoincidenciaDto(string Tipo, string Donde, string Detalle);

public sealed record BusquedaDto(string Texto, int Total, IReadOnlyList<CoincidenciaDto> Resultados);

internal static class Texto
{
    /// <summary>Contiene el texto buscado, sin importar mayúsculas ni tildes.</summary>
    public static bool Contiene(string? texto, string buscado) =>
        texto is not null &&
        CultureInfo.InvariantCulture.CompareInfo.IndexOf(
            texto, buscado, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0;

    public static bool Iguales(string a, string b) =>
        string.Compare(a.Trim(), b.Trim(), CultureInfo.InvariantCulture, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) == 0;
}

internal static class BuscadorDeSistemas
{
    /// <summary>Encuentra un sistema por su nombre ("Pedidos") o su carpeta ("PedidosSystem").</summary>
    public static SistemaInfo Encontrar(IEstructuraSistemas estructura, string nombre) =>
        estructura.Sistemas.FirstOrDefault(s => Texto.Iguales(s.Nombre, nombre) || Texto.Iguales(s.Carpeta, nombre))
        ?? throw new NotFoundException(
            $"No existe el sistema '{nombre}'. Los que hay: {string.Join(", ", estructura.Sistemas.Select(s => s.Nombre))}.");
}

// --- Lista y detalle ------------------------------------------------------------------------------------------------

public sealed record ListarSistemasQuery : IRequest<IReadOnlyList<SistemaResumenDto>>;

public sealed class ListarSistemasQueryHandler(IEstructuraSistemas estructura)
    : IRequestHandler<ListarSistemasQuery, IReadOnlyList<SistemaResumenDto>>
{
    public Task<IReadOnlyList<SistemaResumenDto>> Handle(ListarSistemasQuery request, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<SistemaResumenDto>>([.. estructura.Sistemas.Select(s => new SistemaResumenDto(
            s.Nombre, s.Descripcion, s.PuertoHttps, s.PuertoHttp, s.BaseDeDatos, s.Tecnologias,
            s.Endpoints.Count, [.. s.Consume.Select(c => c.Sistema)]))]);
}

public sealed record ObtenerSistemaQuery(string Nombre) : IRequest<SistemaInfo>;

public sealed class ObtenerSistemaQueryHandler(IEstructuraSistemas estructura) : IRequestHandler<ObtenerSistemaQuery, SistemaInfo>
{
    public Task<SistemaInfo> Handle(ObtenerSistemaQuery request, CancellationToken cancellationToken) =>
        Task.FromResult(BuscadorDeSistemas.Encontrar(estructura, request.Nombre));
}

// --- Impacto --------------------------------------------------------------------------------------------------------

public sealed record ImpactoDeSistemaQuery(string Nombre) : IRequest<ImpactoDto>;

public sealed class ImpactoDeSistemaQueryHandler(IEstructuraSistemas estructura) : IRequestHandler<ImpactoDeSistemaQuery, ImpactoDto>
{
    public Task<ImpactoDto> Handle(ImpactoDeSistemaQuery request, CancellationToken cancellationToken)
    {
        var sistema = BuscadorDeSistemas.Encontrar(estructura, request.Nombre);

        var consumidores = estructura.Sistemas
            .Where(s => s.Nombre != sistema.Nombre)
            .SelectMany(s => s.Consume.Where(c => Texto.Iguales(c.Sistema, sistema.Nombre))
                .Select(c => new ConsumidorDto(s.Nombre, c.Para, c.Como)))
            .ToList();

        var resumen = consumidores.Count == 0
            ? $"Ningún otro sistema llama a {sistema.Nombre}: un cambio ahí no afecta a los demás."
            : $"Si cambiás {sistema.Nombre} pueden verse afectados: {string.Join(", ", consumidores.Select(c => c.Sistema).Distinct())}. " +
              "Revisá los contratos antes de cambiar rutas, DTOs o códigos de respuesta.";

        return Task.FromResult(new ImpactoDto(sistema.Nombre, consumidores, sistema.Contratos, sistema.Notas, resumen));
    }
}

// --- Búsqueda -------------------------------------------------------------------------------------------------------

public sealed record BuscarConocimientoQuery(string Texto) : IRequest<BusquedaDto>;

public sealed class BuscarConocimientoQueryValidator : AbstractValidator<BuscarConocimientoQuery>
{
    public BuscarConocimientoQueryValidator() => RuleFor(q => q.Texto).NotEmpty().Length(2, 100);
}

public sealed class BuscarConocimientoQueryHandler(IEstructuraSistemas estructura, IBibliotecaDocumentos biblioteca)
    : IRequestHandler<BuscarConocimientoQuery, BusquedaDto>
{
    private const int MaximoResultados = 50;

    public Task<BusquedaDto> Handle(BuscarConocimientoQuery request, CancellationToken cancellationToken)
    {
        var texto = request.Texto.Trim();
        var resultados = new List<CoincidenciaDto>();

        foreach (var s in estructura.Sistemas)
        {
            if (Texto.Contiene(s.Nombre, texto) || Texto.Contiene(s.Descripcion, texto) || Texto.Contiene(s.BaseDeDatos, texto) ||
                s.Tecnologias.Any(t => Texto.Contiene(t, texto)))
                resultados.Add(new("Sistema", s.Nombre, s.Descripcion));

            resultados.AddRange(s.Endpoints
                .Where(e => Texto.Contiene(e.Ruta, texto) || Texto.Contiene(e.Descripcion, texto))
                .Select(e => new CoincidenciaDto("Endpoint", s.Nombre, $"{e.Metodo} {e.Ruta}: {e.Descripcion}")));
            resultados.AddRange(s.Capas
                .Where(c => Texto.Contiene(c.Proyecto, texto) || Texto.Contiene(c.Responsabilidad, texto))
                .Select(c => new CoincidenciaDto("Capa", s.Nombre, $"{c.Proyecto}: {c.Responsabilidad}")));
            resultados.AddRange(s.Consume
                .Where(d => Texto.Contiene(d.Sistema, texto) || Texto.Contiene(d.Para, texto) || Texto.Contiene(d.Como, texto))
                .Select(d => new CoincidenciaDto("Dependencia", s.Nombre, $"Usa {d.Sistema}: {d.Para} ({d.Como})")));
            resultados.AddRange(s.Contratos
                .Where(c => Texto.Contiene(c.Nombre, texto) || Texto.Contiene(c.Descripcion, texto))
                .Select(c => new CoincidenciaDto("Contrato", s.Nombre, $"{c.Nombre}: {c.Descripcion}")));
            resultados.AddRange(s.Notas
                .Where(n => Texto.Contiene(n, texto))
                .Select(n => new CoincidenciaDto("Nota", s.Nombre, n)));
        }

        resultados.AddRange(biblioteca.Documentos
            .Where(d => Texto.Contiene(d.Titulo, texto) || Texto.Contiene(d.Resumen, texto) || Texto.Contiene(d.Contenido, texto))
            .Select(d => new CoincidenciaDto(
                d.Tipo == "arquitectura" ? "Arquitectura" : "Instrucciones", $"{d.Tipo}/{d.Id}", $"{d.Titulo}: {d.Resumen}")));

        return Task.FromResult(new BusquedaDto(texto, resultados.Count, [.. resultados.Take(MaximoResultados)]));
    }
}

// --- Documentos -----------------------------------------------------------------------------------------------------

public sealed record ListarDocumentosQuery(string Tipo) : IRequest<IReadOnlyList<DocumentoResumenDto>>;

public sealed class ListarDocumentosQueryHandler(IBibliotecaDocumentos biblioteca)
    : IRequestHandler<ListarDocumentosQuery, IReadOnlyList<DocumentoResumenDto>>
{
    public Task<IReadOnlyList<DocumentoResumenDto>> Handle(ListarDocumentosQuery request, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<DocumentoResumenDto>>([.. biblioteca.Documentos
            .Where(d => d.Tipo == request.Tipo)
            .Select(d => new DocumentoResumenDto(d.Id, d.Titulo, d.Resumen))]);
}

public sealed record ObtenerDocumentoQuery(string Tipo, string Id) : IRequest<DocumentoInfo>;

public sealed class ObtenerDocumentoQueryHandler(IBibliotecaDocumentos biblioteca) : IRequestHandler<ObtenerDocumentoQuery, DocumentoInfo>
{
    public Task<DocumentoInfo> Handle(ObtenerDocumentoQuery request, CancellationToken cancellationToken)
    {
        var documentos = biblioteca.Documentos.Where(d => d.Tipo == request.Tipo).ToList();
        var documento = documentos.FirstOrDefault(d => Texto.Iguales(d.Id, request.Id))
            ?? throw new NotFoundException(
                $"No existe '{request.Id}' en {request.Tipo}. Los que hay: {string.Join(", ", documentos.Select(d => d.Id))}.");
        return Task.FromResult(documento);
    }
}
