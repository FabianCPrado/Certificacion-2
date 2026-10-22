using System.Text.RegularExpressions;
using Asistente.Application.Interfaces;
using Asistente.Domain;

namespace Asistente.Infrastructure.Mapa;

/// <summary>
/// Lee los documentos .md de &lt;carpeta&gt;/arquitectura y &lt;carpeta&gt;/instrucciones. Cada archivo empieza con
/// "# Título", y una línea "&gt; resumen". El id es el nombre del archivo sin el número del orden (01-vision → vision).
/// </summary>
public sealed partial class BibliotecaDocumentos : IBibliotecaDocumentos
{
    public const string Arquitectura = "arquitectura";
    public const string Instrucciones = "instrucciones";

    public BibliotecaDocumentos(string carpetaBase)
    {
        Documentos = [.. LeerCarpeta(carpetaBase, Arquitectura), .. LeerCarpeta(carpetaBase, Instrucciones)];
    }

    public IReadOnlyList<DocumentoInfo> Documentos { get; }

    private static IEnumerable<DocumentoInfo> LeerCarpeta(string carpetaBase, string tipo)
    {
        var carpeta = Path.Combine(carpetaBase, tipo);
        if (!Directory.Exists(carpeta))
            throw new InvalidOperationException($"No existe la carpeta de documentos {carpeta}.");

        return Directory.EnumerateFiles(carpeta, "*.md")
            .Order(StringComparer.OrdinalIgnoreCase)
            .Select(archivo => LeerArchivo(archivo, tipo));
    }

    private static DocumentoInfo LeerArchivo(string archivo, string tipo)
    {
        var contenido = File.ReadAllText(archivo);
        var lineas = contenido.Split('\n').Select(l => l.TrimEnd('\r')).ToList();

        var titulo = lineas.FirstOrDefault(l => l.StartsWith("# ", StringComparison.Ordinal))?[2..].Trim()
            ?? throw new InvalidOperationException($"{archivo} no empieza con un título '# ...'.");
        var resumen = lineas.FirstOrDefault(l => l.StartsWith("> ", StringComparison.Ordinal))?[2..].Trim() ?? string.Empty;
        var id = PrefijoNumerico().Replace(Path.GetFileNameWithoutExtension(archivo), string.Empty);

        return new DocumentoInfo(id, tipo, titulo, resumen, contenido.Trim());
    }

    [GeneratedRegex(@"^\d+-")]
    private static partial Regex PrefijoNumerico();
}
