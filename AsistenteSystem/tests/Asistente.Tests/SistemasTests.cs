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

    private static readonly EstructuraFalsa Estructura = new(Catalogo, Pedidos, Asistente);

    private static readonly BibliotecaFalsa Biblioteca = new(
        new("capas", "arquitectura", "Capas y dependencias", "Cuatro proyectos por sistema", "# Capas\nDomain no referencia a nadie"),
        new("agregar-endpoint", "instrucciones", "Agregar un endpoint", "Pasos de adentro hacia afuera", "# Agregar\n1. Domain\n2. Application"));

    // --- Lista y detalle -----------------------------------------------------------------------------------------------

    [Fact]
    public async Task Lista_los_sistemas_con_su_resumen()
    {
        var lista = await new ListarSistemasQueryHandler(Estructura).Handle(new ListarSistemasQuery(), default);

        Assert.Equal(["Catalogo", "Pedidos", "Asistente"], lista.Select(s => s.Nombre));
        Assert.Equal(1, lista[0].CantidadEndpoints);
        Assert.Equal(["Catalogo", "API de Anthropic"], lista[2].Consume);
    }

    [Theory]
    [InlineData("Pedidos")]
    [InlineData("pedidos")]
    [InlineData("PedidosSystem")]
    [InlineData(" PEDIDOS ")]
    public async Task Encuentra_un_sistema_por_nombre_o_carpeta_sin_importar_mayusculas(string nombre)
    {
        var sistema = await new ObtenerSistemaQueryHandler(Estructura).Handle(new ObtenerSistemaQuery(nombre), default);

        Assert.Equal("Pedidos", sistema.Nombre);
    }

    [Fact]
    public async Task Un_sistema_inexistente_da_404_y_lista_los_validos()
    {
        var ex = await Assert.ThrowsAsync<NotFoundException>(
            () => new ObtenerSistemaQueryHandler(Estructura).Handle(new ObtenerSistemaQuery("Facturas"), default));

        Assert.Contains("Catalogo, Pedidos, Asistente", ex.Message);
    }

    // --- Impacto -------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Impacto_dice_quien_consume_al_sistema_y_que_contratos_usan()
    {
        var impacto = await new ImpactoDeSistemaQueryHandler(Estructura).Handle(new ImpactoDeSistemaQuery("catalogo"), default);

        Assert.Equal(["Pedidos", "Asistente"], impacto.LoConsumen.Select(c => c.Sistema));
        Assert.Equal("ProductoDto", impacto.Contratos.Single().Nombre);
        Assert.Contains("Pedidos, Asistente", impacto.Resumen);
    }

    [Fact]
    public async Task Impacto_de_un_sistema_que_nadie_llama_lo_dice()
    {
        var impacto = await new ImpactoDeSistemaQueryHandler(Estructura).Handle(new ImpactoDeSistemaQuery("Asistente"), default);

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
    public async Task La_busqueda_encuentra_en_cada_tipo_sin_importar_mayusculas_ni_tildes(string texto, string tipo)
    {
        var busqueda = await new BuscarConocimientoQueryHandler(Estructura, Biblioteca).Handle(new BuscarConocimientoQuery(texto), default);

        Assert.Contains(busqueda.Resultados, r => r.Tipo == tipo);
    }

    [Fact]
    public async Task La_busqueda_ignora_tildes()
    {
        // "Pedidós" (con tilde) encuentra al sistema "Pedidos" (sin tilde)
        var busqueda = await new BuscarConocimientoQueryHandler(Estructura, Biblioteca).Handle(new BuscarConocimientoQuery("Pedidós"), default);

        Assert.Contains(busqueda.Resultados, r => r.Tipo == "Sistema" && r.Donde == "Pedidos");
    }

    [Fact]
    public async Task Una_busqueda_sin_resultados_devuelve_total_cero()
    {
        var busqueda = await new BuscarConocimientoQueryHandler(Estructura, Biblioteca).Handle(new BuscarConocimientoQuery("zzzzz"), default);

        Assert.Equal(0, busqueda.Total);
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("a", false)]
    [InlineData("ab", true)]
    public void El_texto_a_buscar_tiene_entre_2_y_100_caracteres(string texto, bool valido) =>
        Assert.Equal(valido, new BuscarConocimientoQueryValidator().Validate(new BuscarConocimientoQuery(texto)).IsValid);

    // --- Documentos ----------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Lista_solo_los_documentos_del_tipo_pedido()
    {
        var lista = await new ListarDocumentosQueryHandler(Biblioteca).Handle(new ListarDocumentosQuery("arquitectura"), default);

        Assert.Equal("capas", lista.Single().Id);
    }

    [Fact]
    public async Task Obtiene_un_documento_completo_y_da_404_si_no_existe()
    {
        var documento = await new ObtenerDocumentoQueryHandler(Biblioteca).Handle(new ObtenerDocumentoQuery("instrucciones", "agregar-endpoint"), default);
        Assert.Contains("2. Application", documento.Contenido);

        await Assert.ThrowsAsync<NotFoundException>(
            () => new ObtenerDocumentoQueryHandler(Biblioteca).Handle(new ObtenerDocumentoQuery("instrucciones", "capas"), default));
    }

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
