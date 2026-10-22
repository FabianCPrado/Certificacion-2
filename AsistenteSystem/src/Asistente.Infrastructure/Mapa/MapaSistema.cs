using System.Text.Json;
using Asistente.Application.Interfaces;
using Asistente.Domain;

namespace Asistente.Infrastructure.Mapa;

/// <summary>Lee mapa-sistema.json. Falla al iniciar si el archivo no existe o está mal formado.</summary>
public sealed class MapaSistema : IMapaSistema
{
    private static readonly JsonSerializerOptions Opciones = new(JsonSerializerDefaults.Web);

    public MapaSistema(string rutaArchivo)
    {
        using var stream = File.OpenRead(rutaArchivo);
        var documento = JsonSerializer.Deserialize<DocumentoMapa>(stream, Opciones)
            ?? throw new InvalidOperationException($"{rutaArchivo} está vacío.");

        if (documento.Herramientas is not { Count: > 0 })
            throw new InvalidOperationException($"{rutaArchivo} no define herramientas.");

        Herramientas = documento.Herramientas;
    }

    public IReadOnlyList<HerramientaInfo> Herramientas { get; }

    private sealed record DocumentoMapa(List<HerramientaInfo>? Herramientas);
}
