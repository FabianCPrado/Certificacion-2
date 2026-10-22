using System.Text.Json;
using Asistente.Application.Exceptions;
using Asistente.Application.Interfaces;
using Asistente.Application.Sistemas;
using Asistente.Domain;
using Asistente.Infrastructure.Mapa;

namespace Asistente.Tests;

internal sealed class EstructuraFalsa(params SistemaInfo[] sistemas) : IEstructuraSistemas
{
    public IReadOnlyList<SistemaInfo> Sistemas { get; } = sistemas;
}

internal sealed class BibliotecaFalsa(params DocumentoInfo[] documentos) : IBibliotecaDocumentos
{
    public IReadOnlyList<DocumentoInfo> Documentos { get; } = documentos;
}

public class SistemasTests
{
    private static SistemaInfo Sistema(
        string nombre, DependenciaInfo[]? consume = null, EndpointInfo[]? endpoints = null, ContratoInfo[]? contratos = null) =>
        new(nombre, $"{nombre}System", $"Sistema {nombre}", 7000, 5000, $"{nombre}Db", ["EF Core"], [],
            endpoints ?? [], consume ?? [], contratos ?? [], [], []);

    private static readonly SistemaInfo Catalogo = Sistema(
        "Catalogo",
        endpoints: [new("GET", "/api/productos/{categoria}/{id}", "Obtiene un producto con su stock", "JWT")],
        contratos: [new("ProductoDto", "id, nombre, precio, stock", ["Pedidos", "Asistente"])]);

    private static readonly SistemaInfo Pedidos = Sistema(
        "Pedidos", consume: [new("Catalogo", "Leer productos", "REST")]);

    private static readonly SistemaInfo Asistente = Sistema(
        "Asistente", consume: [new("Catalogo", "Precio y stock", "REST"), new("API de Anthropic", "El modelo", "HTTPS")]);

    private static readonly DocumentoInfo[] Documentos =
    [
        new("capas", "arquitectura", "Capas y dependencias", "Cuatro proyectos por sistema", "# Capas\nDomain no referencia a nadie"),
        new("agregar-endpoint", "instrucciones", "Agregar un endpoint", "Pasos de adentro hacia afuera", "# Agregar\n1. Domain\n2. Application")
    ];

    private static ConocimientoEcosistema Conocimiento() =>
        new(new EstructuraFalsa(Catalogo, Pedidos, Asistente), new BibliotecaFalsa(Documentos));

    // --- Lista y detalle -----------------------------------------------------------------------------------------------

    [Fact]
    public void Lista_los_sistemas_con_su_resumen()
    {
        var lista = Conocimiento().Listar();

        Assert.Equal(["Catalogo", "Pedidos", "Asistente"], lista.Select(s => s.Nombre));
        Assert.Equal(1, lista[0].CantidadEndpoints);
        Assert.Equal(["Catalogo", "API de Anthropic"], lista[2].Consume);
    }

    [Theory]
    [InlineData("Pedidos")]
    [InlineData("pedidos")]
    [InlineData("PedidosSystem")]
    [InlineData(" PEDIDOS ")]
    public void Encuentra_un_sistema_por_nombre_o_carpeta_sin_importar_mayusculas(string nombre) =>
        Assert.Equal("Pedidos", Conocimiento().Obtener(nombre).Nombre);

    [Fact]
    public void Un_sistema_inexistente_falla_y_lista_los_validos()
    {
        var ex = Assert.Throws<NotFoundException>(() => Conocimiento().Obtener("Facturas"));

        Assert.Contains("Catalogo, Pedidos, Asistente", ex.Message);
    }

    // --- Impacto -------------------------------------------------------------------------------------------------------

    [Fact]
    public void Impacto_dice_quien_consume_al_sistema_y_que_contratos_usan()
    {
        var impacto = Conocimiento().Impacto("catalogo");

        Assert.Equal(["Pedidos", "Asistente"], impacto.LoConsumen.Select(c => c.Sistema));
        Assert.Equal("ProductoDto", impacto.Contratos.Single().Nombre);
        Assert.Contains("Pedidos, Asistente", impacto.Resumen);
    }

    [Fact]
    public void Impacto_de_un_sistema_que_nadie_llama_lo_dice()
    {
        var impacto = Conocimiento().Impacto("Asistente");

        Assert.Empty(impacto.LoConsumen);
        Assert.Contains("Ningún otro sistema", impacto.Resumen);
    }

    // --- Búsqueda ------------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData("STOCK", "Endpoint")]
    [InlineData("stock", "Contrato")]
    [InlineData("anthropic", "Dependencia")]
    [InlineData("dependencias", "Arquitectura")]
    [InlineData("adentro", "Instrucciones")]
    public void La_busqueda_encuentra_en_cada_tipo_sin_importar_mayusculas(string texto, string tipo) =>
        Assert.Contains(Conocimiento().Buscar(texto).Resultados, r => r.Tipo == tipo);

    [Fact]
    public void La_busqueda_ignora_tildes()
    {
        // "Pedidós" (con tilde) encuentra al sistema "Pedidos" (sin tilde)
        var busqueda = Conocimiento().Buscar("Pedidós");

        Assert.Contains(busqueda.Resultados, r => r.Tipo == "Sistema" && r.Donde == "Pedidos");
    }

    [Fact]
    public void Una_busqueda_sin_resultados_devuelve_total_cero() =>
        Assert.Equal(0, Conocimiento().Buscar("zzzzz").Total);

    // --- Documentos ----------------------------------------------------------------------------------------------------

    [Fact]
    public void Lista_los_documentos_de_un_tipo_o_todos()
    {
        Assert.Equal("arquitectura/capas", Conocimiento().Documentos("arquitectura").Single().Id);
        Assert.Equal(2, Conocimiento().Documentos().Count);
    }

    [Theory]
    [InlineData("instrucciones", "agregar-endpoint")]
    [InlineData("INSTRUCCIONES", "instrucciones/agregar-endpoint")]
    public void Obtiene_un_documento_completo_con_o_sin_el_tipo_en_el_id(string tipo, string id) =>
        Assert.Contains("2. Application", Conocimiento().Documento(tipo, id).Contenido);

    [Fact]
    public void Un_documento_de_otro_tipo_no_existe_y_lista_los_validos()
    {
        var ex = Assert.Throws<NotFoundException>(() => Conocimiento().Documento("instrucciones", "capas"));

        Assert.Contains("agregar-endpoint", ex.Message);
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("ab", false)]
    [InlineData("¿Qué sistemas hay?", true)]
    public void La_pregunta_sobre_sistemas_tiene_entre_3_y_500_caracteres(string pregunta, bool valida) =>
        Assert.Equal(valida, new PreguntarSobreSistemasQueryValidator().Validate(new PreguntarSobreSistemasQuery(pregunta)).IsValid);

    // --- Los archivos reales -------------------------------------------------------------------------------------------

    private static string? RaizDelRepositorio()
    {
        for (var carpeta = new DirectoryInfo(AppContext.BaseDirectory); carpeta is not null; carpeta = carpeta.Parent)
        {
            if (File.Exists(Path.Combine(carpeta.FullName, "AsistenteSystem.slnx")))
                return carpeta.Parent?.FullName;
        }

        return null;
    }

    private static string DeAsistente(string relativa) => Path.Combine(RaizDelRepositorio()!, "AsistenteSystem", relativa);

    [Fact]
    public void El_sistemas_json_real_define_los_cinco_sistemas_con_su_contenido()
    {
        if (RaizDelRepositorio() is null) return;

        var sistemas = new EstructuraSistemas(DeAsistente("src/Asistente.Api/sistemas.json")).Sistemas;

        Assert.Equal(["Authentication", "Catalogo", "Pedidos", "Clientes", "Asistente"], sistemas.Select(s => s.Nombre));
        Assert.All(sistemas, s =>
        {
            Assert.NotEmpty(s.Endpoints);
            Assert.NotEmpty(s.Capas);
            Assert.NotEmpty(s.Tests);
        });
    }

    [Fact]
    public void Todo_lo_que_un_sistema_consume_existe_como_sistema_o_es_un_servicio_externo_conocido()
    {
        if (RaizDelRepositorio() is null) return;

        var sistemas = new EstructuraSistemas(DeAsistente("src/Asistente.Api/sistemas.json")).Sistemas;
        var nombres = sistemas.Select(s => s.Nombre).Append("API de Anthropic").ToHashSet();

        foreach (var dependencia in sistemas.SelectMany(s => s.Consume))
            Assert.Contains(dependencia.Sistema, nombres);
    }

    [Fact]
    public void Los_puertos_de_sistemas_json_coinciden_con_los_launchSettings_de_cada_sistema()
    {
        if (RaizDelRepositorio() is null) return;

        var sistemas = new EstructuraSistemas(DeAsistente("src/Asistente.Api/sistemas.json")).Sistemas;

        foreach (var s in sistemas)
        {
            var carpeta = Path.Combine(RaizDelRepositorio()!, s.Carpeta, "src");
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
    public void Los_documentos_reales_se_leen_con_titulo_y_resumen()
    {
        if (RaizDelRepositorio() is null) return;

        var biblioteca = new BibliotecaDocumentos(DeAsistente("docs"));

        Assert.Equal(7, biblioteca.Documentos.Count(d => d.Tipo == "arquitectura"));
        Assert.Equal(6, biblioteca.Documentos.Count(d => d.Tipo == "instrucciones"));
        Assert.All(biblioteca.Documentos, d =>
        {
            Assert.NotEmpty(d.Titulo);
            Assert.NotEmpty(d.Resumen);
            Assert.DoesNotMatch(@"^\d", d.Id);
        });
    }
}
