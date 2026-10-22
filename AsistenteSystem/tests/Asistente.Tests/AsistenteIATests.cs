using Asistente.Application.Asistente;
using Asistente.Application.Exceptions;
using Asistente.Application.Interfaces;
using Asistente.Domain;
using Asistente.Infrastructure.IA;
using Microsoft.Extensions.AI;
using NSubstitute;

namespace Asistente.Tests;

/// <summary>Un modelo de mentira: devuelve las respuestas que se le cargan, en orden.</summary>
internal sealed class ModeloFalso(params ChatResponse[] respuestas) : IChatClient
{
    private readonly Queue<ChatResponse> _respuestas = new(respuestas);

    public List<IList<ChatMessage>> Recibido { get; } = [];

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        Recibido.Add([.. messages]);
        return Task.FromResult(_respuestas.Dequeue());
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}

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
        return new AsistenteIA(new FunctionInvokingChatClient(modelo), herramientas);
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
