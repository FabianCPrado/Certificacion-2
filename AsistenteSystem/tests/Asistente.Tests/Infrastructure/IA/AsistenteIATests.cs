using Asistente.Application.Asistente;
using Asistente.Application.Exceptions;
using Asistente.Application.Interfaces;
using Asistente.Domain;
using Asistente.Infrastructure.IA;
using Asistente.Tests.Application.Fakes;
using Asistente.Tests.Infrastructure.Fakes;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Asistente.Tests.Infrastructure.IA;

public class AsistenteIATests
{
    private static readonly string[] Nombres =
    [
        "ObtenerPrecioYStock", "BuscarProductos", "UltimosPedidosDeCliente", "EstadoDePedido", "DatosDeCliente"
    ];

    private readonly ICatalogoConsultas _catalogo = Substitute.For<ICatalogoConsultas>();

    private static ChatResponse Texto(string texto) => new(new ChatMessage(ChatRole.Assistant, texto));

    private static ChatResponse LlamaA(string herramienta, Dictionary<string, object?> argumentos) =>
        new(new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("llamada-1", herramienta, argumentos)]));

    // FunctionInvokingChatClient es el mismo que usa la aplicación: ejecuta las herramientas que pide el modelo
    private AsistenteIA Crear(ModeloFalso modelo)
    {
        var mapa = new MapaFalso([.. Nombres.Select(n => new HerramientaInfo(n, "d", "s", "GET /x", true))]);
        var herramientas = new Herramientas(mapa, _catalogo, Substitute.For<IPedidosConsultas>(), Substitute.For<IClientesConsultas>());
        return new AsistenteIA(new FunctionInvokingChatClient(modelo), herramientas, "modelo-de-prueba", NullLogger<AsistenteIA>.Instance);
    }

    [Fact]
    public async Task Pregunta_de_stock_usa_la_herramienta_y_devuelve_la_fuente()
    {
        _catalogo.ObtenerAsync("Abarrotes", "yerba-1", Arg.Any<CancellationToken>())
            .Returns(new ProductoInfo("Abarrotes", "yerba-1", "Yerba mate 1 kg", 4.20m, 12));
        var modelo = new ModeloFalso(
            LlamaA("ObtenerPrecioYStock", new() { ["categoria"] = "Abarrotes", ["id"] = "yerba-1" }),
            Texto("Sí, hay 12 unidades de yerba."));

        var respuesta = await Crear(modelo).ResponderAsync(new PreguntaRequest("¿Hay stock de yerba?", null));

        Assert.Equal("Sí, hay 12 unidades de yerba.", respuesta.Respuesta);
        Assert.Equal(["ObtenerPrecioYStock"], respuesta.HerramientasUsadas);
        Assert.Equal([new Fuente("Catalogo", "Abarrotes/yerba-1")], respuesta.Fuentes);
    }

    private static ChatResponse ConUso(ChatResponse respuesta, long entrada, long salida)
    {
        respuesta.Usage = new UsageDetails { InputTokenCount = entrada, OutputTokenCount = salida };
        return respuesta;
    }

    [Fact]
    public async Task La_respuesta_informa_el_consumo_sumando_todas_las_llamadas_al_modelo()
    {
        _catalogo.ObtenerAsync("Abarrotes", "yerba-1", Arg.Any<CancellationToken>())
            .Returns(new ProductoInfo("Abarrotes", "yerba-1", "Yerba mate 1 kg", 4.20m, 12));
        // Una pregunta con herramienta hace dos llamadas al modelo, y las dos se facturan
        var modelo = new ModeloFalso(
            ConUso(LlamaA("ObtenerPrecioYStock", new() { ["categoria"] = "Abarrotes", ["id"] = "yerba-1" }), 100, 20),
            ConUso(Texto("Hay 12 unidades."), 150, 30));

        var respuesta = await Crear(modelo).ResponderAsync(new PreguntaRequest("¿Hay yerba?", null));

        Assert.Equal("modelo-de-prueba", respuesta.Uso!.Modelo);
        Assert.Equal(250, respuesta.Uso.TokensEntrada);
        Assert.Equal(50, respuesta.Uso.TokensSalida);
    }

    [Fact]
    public async Task Si_el_modelo_no_informa_el_consumo_los_tokens_quedan_sin_dato()
    {
        var respuesta = await Crear(new ModeloFalso(Texto("hola"))).ResponderAsync(new PreguntaRequest("hola", null));

        Assert.Null(respuesta.Uso!.TokensEntrada);
        Assert.Null(respuesta.Uso.TokensSalida);
    }

    [Fact]
    public async Task El_modelo_recibe_las_instrucciones_de_sistema_y_el_id_del_cliente()
    {
        var modelo = new ModeloFalso(Texto("ok"));
        var clienteId = Guid.NewGuid();

        await Crear(modelo).ResponderAsync(new PreguntaRequest("¿Cómo viene mi pedido?", clienteId));

        var enviados = modelo.Recibido.Single();
        Assert.Equal(ChatRole.System, enviados[0].Role);
        Assert.Equal(Instrucciones.Sistema, enviados[0].Text);
        Assert.Contains(clienteId.ToString(), enviados[1].Text);
    }

    [Fact]
    public async Task Sin_herramientas_usadas_la_respuesta_no_tiene_fuentes()
    {
        var respuesta = await Crear(new ModeloFalso(Texto("No tengo esa información."))).ResponderAsync(new PreguntaRequest("hola", null));

        Assert.Empty(respuesta.HerramientasUsadas);
        Assert.Empty(respuesta.Fuentes);
    }

    [Fact]
    public async Task Producto_que_no_existe_se_lo_cuenta_al_modelo_y_no_inventa_fuente()
    {
        _catalogo.ObtenerAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((ProductoInfo?)null);
        var modelo = new ModeloFalso(
            LlamaA("ObtenerPrecioYStock", new() { ["categoria"] = "Abarrotes", ["id"] = "nada" }),
            Texto("No encontré ese producto."));

        var respuesta = await Crear(modelo).ResponderAsync(new PreguntaRequest("¿Hay nada?", null));

        Assert.Empty(respuesta.Fuentes);
        Assert.Equal(["ObtenerPrecioYStock"], respuesta.HerramientasUsadas);
    }

    [Fact]
    public async Task Si_Catalogo_esta_caido_responde_servicio_no_disponible()
    {
        _catalogo.ObtenerAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<ProductoInfo?>(_ => throw new ServicioNoDisponibleException("Catálogo no responde."));
        var modelo = new ModeloFalso(LlamaA("ObtenerPrecioYStock", new() { ["categoria"] = "Abarrotes", ["id"] = "x" }));

        await Assert.ThrowsAsync<ServicioNoDisponibleException>(
            () => Crear(modelo).ResponderAsync(new PreguntaRequest("¿Hay stock?", null)));
    }
}
