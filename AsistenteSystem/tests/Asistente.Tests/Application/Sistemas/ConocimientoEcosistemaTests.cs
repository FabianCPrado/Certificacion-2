using Asistente.Application.Exceptions;
using Asistente.Application.Sistemas;
using Asistente.Domain;
using Asistente.Tests.Application.Fakes;

namespace Asistente.Tests.Application.Sistemas;

public class ConocimientoEcosistemaTests
{
    private static SistemaInfo Sistema(
        string nombre, DependenciaInfo[]? consume = null, EndpointInfo[]? endpoints = null, ContratoInfo[]? contratos = null) =>
        new(nombre, $"{nombre}System", $"Sistema {nombre}", 7000, 5000, $"{nombre}Db", ["EF Core"], [],
            endpoints ?? [], consume ?? [], contratos ?? [], [], []);

    private static readonly SistemaInfo Catalogo = Sistema(
        "Catalogo",
        endpoints: [new("GET", "/api/productos/{categoria}/{id}", "Obtiene un producto con su stock", "JWT")],
        contratos: [new("ProductoDto", "id, nombre, precio, stock", ["Pedidos", "Reportes"])]);

    private static readonly SistemaInfo Pedidos = Sistema(
        "Pedidos", consume: [new("Catalogo", "Leer productos", "REST")]);

    private static readonly SistemaInfo Reportes = Sistema(
        "Reportes", consume: [new("Catalogo", "Precio y stock", "REST"), new("API externa", "Un tercero", "HTTPS")]);

    private static readonly DocumentoInfo[] Documentos =
    [
        new("capas", "arquitectura", "Capas y dependencias", "Cuatro proyectos por sistema", "# Capas\nDomain no referencia a nadie"),
        new("agregar-endpoint", "instrucciones", "Agregar un endpoint", "Pasos de adentro hacia afuera", "# Agregar\n1. Domain\n2. Application")
    ];

    private static ConocimientoEcosistema Conocimiento() =>
        new(new EstructuraFalsa(Catalogo, Pedidos, Reportes), new BibliotecaFalsa(Documentos));

    // --- Lista y detalle -----------------------------------------------------------------------------------------------

    [Fact]
    public void Lista_los_sistemas_con_su_resumen()
    {
        var lista = Conocimiento().Listar();

        Assert.Equal(["Catalogo", "Pedidos", "Reportes"], lista.Select(s => s.Nombre));
        Assert.Equal(1, lista[0].CantidadEndpoints);
        Assert.Equal(["Catalogo", "API externa"], lista[2].Consume);
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

        Assert.Contains("Catalogo, Pedidos, Reportes", ex.Message);
    }

    // --- Impacto -------------------------------------------------------------------------------------------------------

    [Fact]
    public void Impacto_dice_quien_consume_al_sistema_y_que_contratos_usan()
    {
        var impacto = Conocimiento().Impacto("catalogo");

        Assert.Equal(["Pedidos", "Reportes"], impacto.LoConsumen.Select(c => c.Sistema));
        Assert.Equal("ProductoDto", impacto.Contratos.Single().Nombre);
        Assert.Contains("Pedidos, Reportes", impacto.Resumen);
    }

    [Fact]
    public void Impacto_de_un_sistema_que_nadie_llama_lo_dice()
    {
        var impacto = Conocimiento().Impacto("Reportes");

        Assert.Empty(impacto.LoConsumen);
        Assert.Contains("Ningún otro sistema", impacto.Resumen);
    }

    // --- Búsqueda ------------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData("STOCK", "Endpoint")]
    [InlineData("stock", "Contrato")]
    [InlineData("externa", "Dependencia")]
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
}
