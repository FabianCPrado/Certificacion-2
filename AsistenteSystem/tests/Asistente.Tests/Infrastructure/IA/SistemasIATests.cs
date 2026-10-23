using Asistente.Application.Exceptions;
using Asistente.Application.Sistemas;
using Asistente.Domain;
using Asistente.Infrastructure.IA;
using Asistente.Tests.Application.Fakes;
using Asistente.Tests.Infrastructure.Fakes;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;

namespace Asistente.Tests.Infrastructure.IA;

public class SistemasIATests
{
    private static readonly SistemaInfo Pedidos = new(
        "Pedidos", "PedidosSystem", "Pedidos y líneas", 7101, 5101, "PedidosDb", [], [], [], [], [], [], []);

    private static readonly DocumentoInfo Capas = new(
        "capas-y-dependencias", "arquitectura", "Capas y dependencias", "Cuatro proyectos por sistema", "# Capas\nDomain no referencia a nadie");

    private static ChatResponse Texto(string texto) => new(new ChatMessage(ChatRole.Assistant, texto));

    private static ChatResponse LlamaA(string herramienta, Dictionary<string, object?> argumentos) =>
        new(new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("llamada-1", herramienta, argumentos)]));

    private static SistemasIA Crear(ModeloFalso modelo)
    {
        var conocimiento = new ConocimientoEcosistema(new EstructuraFalsa(Pedidos), new BibliotecaFalsa(Capas));
        return new SistemasIA(
            new FunctionInvokingChatClient(modelo), new HerramientasSistemas(conocimiento), "modelo-de-prueba", NullLogger<SistemasIA>.Instance);
    }

    [Fact]
    public async Task Responde_consultando_la_estructura_y_cita_la_fuente()
    {
        var modelo = new ModeloFalso(
            LlamaA("ObtenerSistema", new() { ["nombre"] = "pedidos" }),
            Texto("Pedidos usa PedidosDb en el puerto 7101."));

        var respuesta = await Crear(modelo).PreguntarAsync("¿Qué puerto usa Pedidos?");

        Assert.Equal("Pedidos usa PedidosDb en el puerto 7101.", respuesta.Respuesta);
        Assert.Equal(["ObtenerSistema"], respuesta.HerramientasUsadas);
        Assert.Equal([new Fuente("Sistemas", "Pedidos")], respuesta.Fuentes);
        Assert.Equal("modelo-de-prueba", respuesta.Uso!.Modelo);
    }

    [Fact]
    public async Task Lee_un_documento_de_arquitectura_y_lo_cita()
    {
        var modelo = new ModeloFalso(
            LlamaA("LeerDocumento", new() { ["tipo"] = "arquitectura", ["id"] = "capas-y-dependencias" }),
            Texto("Domain no referencia a nadie."));

        var respuesta = await Crear(modelo).PreguntarAsync("¿Qué puede referenciar Domain?");

        Assert.Equal([new Fuente("Arquitectura", "capas-y-dependencias")], respuesta.Fuentes);
    }

    [Fact]
    public async Task Un_sistema_inexistente_se_le_cuenta_al_modelo_con_los_nombres_validos_y_no_falla()
    {
        var modelo = new ModeloFalso(
            LlamaA("ObtenerSistema", new() { ["nombre"] = "Facturas" }),
            Texto("No existe ese sistema."));

        var respuesta = await Crear(modelo).PreguntarAsync("¿Cómo es Facturas?");

        Assert.Equal("No existe ese sistema.", respuesta.Respuesta);
        Assert.Empty(respuesta.Fuentes);
        // La segunda llamada al modelo recibió el motivo del error, con los nombres válidos
        Assert.Contains(modelo.Recibido[1].SelectMany(m => m.Contents).OfType<FunctionResultContent>(),
            r => r.Result?.ToString()!.Contains("Los que hay: Pedidos") == true);
    }

    [Fact]
    public void Las_instrucciones_no_nombran_las_herramientas_para_que_el_modelo_no_las_repita_en_sus_respuestas()
    {
        var nombres = new[]
        {
            HerramientasSistemas.ListarSistemas, HerramientasSistemas.ObtenerSistema, HerramientasSistemas.ImpactoDeSistema,
            HerramientasSistemas.BuscarEnConocimiento, HerramientasSistemas.ListarDocumentos, HerramientasSistemas.LeerDocumento,
            Herramientas.ObtenerPrecioYStock, Herramientas.BuscarProductos, Herramientas.UltimosPedidosDeCliente,
            Herramientas.EstadoDePedido, Herramientas.DatosDeCliente
        };

        foreach (var nombre in nombres)
        {
            Assert.False(Instrucciones.Ecosistema.Contains(nombre, StringComparison.Ordinal), $"Ecosistema nombra {nombre}");
            Assert.False(Instrucciones.Sistema.Contains(nombre, StringComparison.Ordinal), $"Sistema nombra {nombre}");
        }
    }

    [Theory]
    [InlineData(nameof(Instrucciones.Ecosistema))]
    [InlineData(nameof(Instrucciones.Sistema))]
    public void Las_instrucciones_prohiben_contar_como_se_obtuvieron_los_datos(string cual)
    {
        var texto = cual == nameof(Instrucciones.Ecosistema) ? Instrucciones.Ecosistema : Instrucciones.Sistema;

        Assert.Contains("Nunca nombres las herramientas", texto);
    }

    [Fact]
    public async Task El_modelo_recibe_las_instrucciones_del_ecosistema_y_no_las_de_negocio()
    {
        var modelo = new ModeloFalso(Texto("ok"));

        await Crear(modelo).PreguntarAsync("¿Qué sistemas hay?");

        var enviados = modelo.Recibido.Single();
        Assert.Equal(Instrucciones.Ecosistema, enviados[0].Text);
        Assert.NotEqual(Instrucciones.Sistema, enviados[0].Text);
    }

    [Fact]
    public async Task Sin_modelo_configurado_responde_servicio_no_disponible() =>
        await Assert.ThrowsAsync<ServicioNoDisponibleException>(() => new SistemasNoConfigurado().PreguntarAsync("hola"));
}
