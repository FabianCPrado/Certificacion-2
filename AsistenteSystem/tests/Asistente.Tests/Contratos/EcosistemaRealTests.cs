using System.Text.Json;
using System.Text.RegularExpressions;
using Asistente.Infrastructure.IA;
using Asistente.Infrastructure.Mapa;

namespace Asistente.Tests.Contratos;

/// <summary>No prueban una clase: prueban que los archivos reales del repositorio son coherentes entre sí.</summary>
public class EcosistemaRealTests
{
    [Fact]
    public void Los_puertos_de_sistemas_json_coinciden_con_los_launchSettings_de_cada_sistema()
    {
        if (RepositorioReal.Raiz() is null) return;

        var sistemas = new EstructuraSistemas(RepositorioReal.DeAsistente("src/Asistente.Api/sistemas.json")).Sistemas;

        foreach (var s in sistemas)
        {
            var carpeta = Path.Combine(RepositorioReal.Raiz()!, s.Carpeta, "src");
            var launch = Directory.GetFiles(carpeta, "launchSettings.json", SearchOption.AllDirectories)
                .FirstOrDefault(f => f.Contains($"{s.Nombre}.Api", StringComparison.Ordinal));
            Assert.True(launch is not null, $"No se encontró launchSettings.json de {s.Nombre}.");

            using var json = JsonDocument.Parse(File.ReadAllText(launch));
            var urls = json.RootElement.GetProperty("profiles").EnumerateObject()
                .Select(p => p.Value.TryGetProperty("applicationUrl", out var u) ? u.GetString() : null)
                .First(u => u is not null)!;

            Assert.Contains($"https://localhost:{s.PuertoHttps}", urls);
            Assert.Contains($"http://localhost:{s.PuertoHttp}", urls);
        }
    }

    [Fact]
    public void La_base_de_conocimiento_del_ecosistema_no_menciona_al_asistente_ni_a_la_IA_que_responde()
    {
        if (RepositorioReal.Raiz() is null) return;

        // El ecosistema son solo los sistemas de negocio: ni el sistema ni las guías hablan del asistente que responde
        var prohibidas = new[] { "asistente", "anthropic", "ia:apikey", "api/sistemas", "api/asistente" };
        var textos = new List<(string Origen, string Texto)>
        {
            ("sistemas.json", File.ReadAllText(RepositorioReal.DeAsistente("src/Asistente.Api/sistemas.json"))),
            ("Instrucciones.Ecosistema", Instrucciones.Ecosistema)
        };
        textos.AddRange(new BibliotecaDocumentos(RepositorioReal.DeAsistente("docs")).Documentos
            .Select(d => ($"{d.Tipo}/{d.Id}", d.Contenido)));

        foreach (var (origen, texto) in textos)
        {
            foreach (var palabra in prohibidas.Concat(origen == "Instrucciones.Ecosistema" ? [] : ["herramienta"]))
                Assert.False(texto.Contains(palabra, StringComparison.OrdinalIgnoreCase), $"{origen} menciona '{palabra}'");
            Assert.False(Regex.IsMatch(texto, "claude(?! code)", RegexOptions.IgnoreCase),
                $"{origen} menciona a Claude");
        }
    }

    [Fact]
    public void El_mapa_real_coincide_con_el_codigo()
    {
        var ruta = Path.Combine(AppContext.BaseDirectory, "mapa-sistema.json");
        if (!File.Exists(ruta))
            ruta = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/Asistente.Api/mapa-sistema.json"));

        var mapa = new MapaSistema(ruta);

        Herramientas.Validar(mapa.Herramientas);
        Assert.All(mapa.Herramientas, h => Assert.True(h.SoloLectura));
    }
}
