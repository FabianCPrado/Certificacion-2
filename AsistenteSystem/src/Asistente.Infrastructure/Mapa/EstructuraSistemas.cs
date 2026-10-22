using System.Text.Json;
using Asistente.Application.Interfaces;
using Asistente.Domain;

namespace Asistente.Infrastructure.Mapa;

/// <summary>Lee sistemas.json. Falla al iniciar si el archivo no existe, está mal formado o repite un sistema.</summary>
public sealed class EstructuraSistemas : IEstructuraSistemas
{
    private static readonly JsonSerializerOptions Opciones = new(JsonSerializerDefaults.Web);

    public EstructuraSistemas(string rutaArchivo)
    {
        using var stream = File.OpenRead(rutaArchivo);
        var documento = JsonSerializer.Deserialize<DocumentoSistemas>(stream, Opciones)
            ?? throw new InvalidOperationException($"{rutaArchivo} está vacío.");

        if (documento.Sistemas is not { Count: > 0 })
            throw new InvalidOperationException($"{rutaArchivo} no define sistemas.");

        var repetido = documento.Sistemas.GroupBy(s => s.Nombre, StringComparer.OrdinalIgnoreCase).FirstOrDefault(g => g.Count() > 1);
        if (repetido is not null)
            throw new InvalidOperationException($"{rutaArchivo} repite el sistema '{repetido.Key}'.");

        Sistemas = documento.Sistemas;
    }

    public IReadOnlyList<SistemaInfo> Sistemas { get; }

    private sealed record DocumentoSistemas(List<SistemaInfo>? Sistemas);
}
