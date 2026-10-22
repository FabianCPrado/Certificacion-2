using System.Globalization;
using Asistente.Application.Exceptions;
using Asistente.Application.Interfaces;
using Asistente.Domain;

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

/// <summary>
/// Lo que se sabe del ecosistema: la estructura de cada sistema (sistemas.json) y los documentos de arquitectura
/// e instrucciones. Son las consultas que la IA usa como herramientas para responder sobre los sistemas.
/// </summary>
public sealed class ConocimientoEcosistema(IEstructuraSistemas estructura, IBibliotecaDocumentos biblioteca)
{
    private const int MaximoResultados = 50;

    public IReadOnlyList<SistemaResumenDto> Listar() =>
        [.. estructura.Sistemas.Select(s => new SistemaResumenDto(
            s.Nombre, s.Descripcion, s.PuertoHttps, s.PuertoHttp, s.BaseDeDatos, s.Tecnologias,
            s.Endpoints.Count, [.. s.Consume.Select(c => c.Sistema)]))];

    /// <summary>La estructura completa de un sistema, por su nombre ("Pedidos") o carpeta ("PedidosSystem").</summary>
    public SistemaInfo Obtener(string nombre) =>
        estructura.Sistemas.FirstOrDefault(s => Iguales(s.Nombre, nombre) || Iguales(s.Carpeta, nombre))
        ?? throw new NotFoundException(
            $"No existe el sistema '{nombre}'. Los que hay: {string.Join(", ", estructura.Sistemas.Select(s => s.Nombre))}.");

    public ImpactoDto Impacto(string nombre)
    {
        var sistema = Obtener(nombre);

        var consumidores = estructura.Sistemas
            .Where(s => s.Nombre != sistema.Nombre)
            .SelectMany(s => s.Consume.Where(c => Iguales(c.Sistema, sistema.Nombre))
                .Select(c => new ConsumidorDto(s.Nombre, c.Para, c.Como)))
            .ToList();

        var resumen = consumidores.Count == 0
            ? $"Ningún otro sistema llama a {sistema.Nombre}: un cambio ahí no afecta a los demás."
            : $"Si cambiás {sistema.Nombre} pueden verse afectados: {string.Join(", ", consumidores.Select(c => c.Sistema).Distinct())}. " +
              "Revisá los contratos antes de cambiar rutas, DTOs o códigos de respuesta.";

        return new ImpactoDto(sistema.Nombre, consumidores, sistema.Contratos, sistema.Notas, resumen);
    }

    /// <summary>Busca un texto en la estructura, la arquitectura y las instrucciones, sin importar tildes ni mayúsculas.</summary>
    public BusquedaDto Buscar(string texto)
    {
        texto = texto.Trim();
        var resultados = new List<CoincidenciaDto>();

        foreach (var s in estructura.Sistemas)
        {
            if (Contiene(s.Nombre, texto) || Contiene(s.Descripcion, texto) || Contiene(s.BaseDeDatos, texto) ||
                s.Tecnologias.Any(t => Contiene(t, texto)))
                resultados.Add(new("Sistema", s.Nombre, s.Descripcion));

            resultados.AddRange(s.Endpoints
                .Where(e => Contiene(e.Ruta, texto) || Contiene(e.Descripcion, texto))
                .Select(e => new CoincidenciaDto("Endpoint", s.Nombre, $"{e.Metodo} {e.Ruta}: {e.Descripcion}")));
            resultados.AddRange(s.Capas
                .Where(c => Contiene(c.Proyecto, texto) || Contiene(c.Responsabilidad, texto))
                .Select(c => new CoincidenciaDto("Capa", s.Nombre, $"{c.Proyecto}: {c.Responsabilidad}")));
            resultados.AddRange(s.Consume
                .Where(d => Contiene(d.Sistema, texto) || Contiene(d.Para, texto) || Contiene(d.Como, texto))
                .Select(d => new CoincidenciaDto("Dependencia", s.Nombre, $"Usa {d.Sistema}: {d.Para} ({d.Como})")));
            resultados.AddRange(s.Contratos
                .Where(c => Contiene(c.Nombre, texto) || Contiene(c.Descripcion, texto))
                .Select(c => new CoincidenciaDto("Contrato", s.Nombre, $"{c.Nombre}: {c.Descripcion}")));
            resultados.AddRange(s.Notas
                .Where(n => Contiene(n, texto))
                .Select(n => new CoincidenciaDto("Nota", s.Nombre, n)));
        }

        resultados.AddRange(biblioteca.Documentos
            .Where(d => Contiene(d.Titulo, texto) || Contiene(d.Resumen, texto) || Contiene(d.Contenido, texto))
            .Select(d => new CoincidenciaDto(
                d.Tipo == "arquitectura" ? "Arquitectura" : "Instrucciones", $"{d.Tipo}/{d.Id}", $"{d.Titulo}: {d.Resumen}")));

        return new BusquedaDto(texto, resultados.Count, [.. resultados.Take(MaximoResultados)]);
    }

    /// <summary>Los documentos de un tipo ("arquitectura" o "instrucciones"), o todos si no se indica.</summary>
    public IReadOnlyList<DocumentoResumenDto> Documentos(string? tipo = null) =>
        [.. biblioteca.Documentos
            .Where(d => string.IsNullOrWhiteSpace(tipo) || Iguales(d.Tipo, tipo))
            .Select(d => new DocumentoResumenDto($"{d.Tipo}/{d.Id}", d.Titulo, d.Resumen))];

    public DocumentoInfo Documento(string tipo, string id)
    {
        // Acepta "capas-y-dependencias" y también "arquitectura/capas-y-dependencias"
        id = id.Contains('/') ? id[(id.LastIndexOf('/') + 1)..] : id;
        var delTipo = biblioteca.Documentos.Where(d => Iguales(d.Tipo, tipo)).ToList();

        return delTipo.FirstOrDefault(d => Iguales(d.Id, id))
            ?? throw new NotFoundException(
                $"No existe '{id}' en {tipo}. Los que hay: {string.Join(", ", delTipo.Select(d => d.Id))}.");
    }

    private static bool Contiene(string? texto, string buscado) =>
        texto is not null &&
        CultureInfo.InvariantCulture.CompareInfo.IndexOf(
            texto, buscado, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0;

    private static bool Iguales(string a, string b) =>
        string.Compare(a.Trim(), b.Trim(), CultureInfo.InvariantCulture, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) == 0;
}
