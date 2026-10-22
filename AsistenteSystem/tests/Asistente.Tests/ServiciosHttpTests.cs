using System.Net;
using System.Text;
using Asistente.Application.Exceptions;
using Asistente.Infrastructure.Servicios;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Asistente.Tests;

/// <summary>Un servidor de mentira: responde lo que se le indica y guarda lo que recibió.</summary>
internal sealed class ServidorFalso(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
{
    public List<HttpRequestMessage> Pedidos { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Pedidos.Add(request);
        return Task.FromResult(responder(request));
    }

    public static HttpResponseMessage Json(string json, HttpStatusCode estado = HttpStatusCode.OK) =>
        new(estado) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
}

internal sealed class TokenFalso : ITokenProvider
{
    public Task<TokenInfo> ObtenerAsync(bool renovar = false, CancellationToken cancellationToken = default) =>
        Task.FromResult(new TokenInfo("jwt-de-prueba", DateTimeOffset.UtcNow.AddHours(1)));
}

internal sealed class RelojFalso(DateTimeOffset inicio) : TimeProvider
{
    public DateTimeOffset Ahora { get; set; } = inicio;

    public override DateTimeOffset GetUtcNow() => Ahora;
}

public class ServiciosHttpTests
{
    private static ServiceProvider ConToken(ServidorFalso servidor, string cliente)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ITokenProvider>(new TokenFalso());
        services.AddTransient<BearerTokenHandler>();
        services.AddHttpClient(cliente, c => c.BaseAddress = new Uri("http://servicio.test"))
            .AddHttpMessageHandler<BearerTokenHandler>()
            .ConfigurePrimaryHttpMessageHandler(() => servidor);
        return services.BuildServiceProvider();
    }

    private static ServiceProvider SinToken(ServidorFalso servidor, string cliente)
    {
        var services = new ServiceCollection();
        services.AddHttpClient(cliente, c => c.BaseAddress = new Uri("http://servicio.test"))
            .ConfigurePrimaryHttpMessageHandler(() => servidor);
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Las_llamadas_a_Catalogo_llevan_el_header_Authorization_con_el_JWT()
    {
        var servidor = new ServidorFalso(_ => ServidorFalso.Json(
            """{"id":"y1","categoria":"Abarrotes","nombre":"Yerba mate 1 kg","precio":4.20,"stock":12,"atributos":[]}"""));
        var consultas = new CatalogoConsultasHttp(ConToken(servidor, VerificadorSalud.Catalogo).GetRequiredService<IHttpClientFactory>());

        var producto = await consultas.ObtenerAsync("Abarrotes", "y1");

        Assert.Equal("Bearer jwt-de-prueba", servidor.Pedidos.Single().Headers.Authorization?.ToString());
        Assert.Equal("/api/productos/Abarrotes/y1", servidor.Pedidos.Single().RequestUri!.AbsolutePath);
        Assert.Equal(new Asistente.Application.Asistente.ProductoInfo("Abarrotes", "y1", "Yerba mate 1 kg", 4.20m, 12), producto);
    }

    private const string CatalogoDePrueba = """
        [{"id":"c1","categoria":"Abarrotes","nombre":"Cafe molido 500 g","precio":5.90,"stock":0,"atributos":[]},
         {"id":"y1","categoria":"Abarrotes","nombre":"Yerba mate 1 kg","precio":4.20,"stock":12,"atributos":[]},
         {"id":"l1","categoria":"Lacteos","nombre":"Leche entera 1 L","precio":1.55,"stock":40,"atributos":[]}]
        """;

    [Theory]
    [InlineData("café")]
    [InlineData("CAFE")]
    [InlineData("molido")]
    public async Task La_busqueda_ignora_tildes_y_mayusculas_y_no_exige_categoria(string buscado)
    {
        var servidor = new ServidorFalso(_ => ServidorFalso.Json(CatalogoDePrueba));
        var consultas = new CatalogoConsultasHttp(ConToken(servidor, VerificadorSalud.Catalogo).GetRequiredService<IHttpClientFactory>());

        var encontrados = await consultas.BuscarAsync(null, buscado);

        Assert.Equal("Cafe molido 500 g", encontrados.Single().Nombre);
        Assert.Equal(0, encontrados.Single().Stock);
    }

    [Fact]
    public async Task La_busqueda_respeta_la_categoria_cuando_se_indica()
    {
        var servidor = new ServidorFalso(_ => ServidorFalso.Json(CatalogoDePrueba));
        var consultas = new CatalogoConsultasHttp(ConToken(servidor, VerificadorSalud.Catalogo).GetRequiredService<IHttpClientFactory>());

        Assert.Empty(await consultas.BuscarAsync("Lacteos", "yerba"));
        Assert.Single(await consultas.BuscarAsync("lácteos", "leche"));
    }

    [Fact]
    public async Task Producto_inexistente_devuelve_null()
    {
        var servidor = new ServidorFalso(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var consultas = new CatalogoConsultasHttp(ConToken(servidor, VerificadorSalud.Catalogo).GetRequiredService<IHttpClientFactory>());

        Assert.Null(await consultas.ObtenerAsync("Abarrotes", "no-existe"));
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task Un_error_del_servicio_se_traduce_a_servicio_no_disponible(HttpStatusCode estado)
    {
        var servidor = new ServidorFalso(_ => new HttpResponseMessage(estado));
        var consultas = new CatalogoConsultasHttp(ConToken(servidor, VerificadorSalud.Catalogo).GetRequiredService<IHttpClientFactory>());

        await Assert.ThrowsAsync<ServicioNoDisponibleException>(() => consultas.BuscarAsync("Abarrotes", "yerba"));
    }

    [Fact]
    public async Task El_email_del_cliente_no_llega_al_modelo()
    {
        var id = Guid.NewGuid();
        var servidor = new ServidorFalso(_ => ServidorFalso.Json($$"""{"id":"{{id}}","nombre":"Cliente 1","email":"cliente1@mail.com"}"""));
        var consultas = new ClientesConsultasHttp(SinToken(servidor, VerificadorSalud.Clientes).GetRequiredService<IHttpClientFactory>());

        var cliente = await consultas.ObtenerAsync(id);

        Assert.Equal("Cliente 1", cliente!.Nombre);
        Assert.DoesNotContain("email", System.Text.Json.JsonSerializer.Serialize(cliente), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Los_ultimos_pedidos_traen_lineas_y_no_traen_email()
    {
        var cliente = Guid.NewGuid();
        var servidor = new ServidorFalso(_ => ServidorFalso.Json($$"""
            {"items":[{"id":"{{Guid.NewGuid()}}","clienteId":"{{cliente}}","estado":"Confirmado","total":10.5,"moneda":"COP",
            "creadoEn":"2026-10-01T10:00:00","lineas":[{"productoId":"y1","categoria":"Abarrotes","nombreProducto":"Yerba","precioUnitario":4.2,"cantidad":2,"subtotal":8.4}]}],
            "pagina":1,"tamanio":5,"total":1}
            """));
        var consultas = new PedidosConsultasHttp(SinToken(servidor, VerificadorSalud.Pedidos).GetRequiredService<IHttpClientFactory>());

        var pedidos = await consultas.UltimosDeClienteAsync(cliente, 5);

        Assert.Equal("Abarrotes", pedidos.Single().Lineas.Single().Categoria);
        Assert.Contains($"clienteId={cliente}", servidor.Pedidos.Single().RequestUri!.Query);
        Assert.Contains("tamanio=5", servidor.Pedidos.Single().RequestUri!.Query);
    }

    // --- TokenProvider -------------------------------------------------------------------------------------

    private static (TokenProvider Proveedor, ServidorFalso Servidor, RelojFalso Reloj) CrearTokenProvider(DateTimeOffset vence)
    {
        var reloj = new RelojFalso(new DateTimeOffset(2026, 10, 22, 12, 0, 0, TimeSpan.Zero));
        var contador = 0;
        var servidor = new ServidorFalso(_ => ServidorFalso.Json(
            $$"""{"accessToken":"jwt-{{++contador}}","tokenType":"Bearer","expiresAt":"{{vence:O}}"}"""));

        var services = new ServiceCollection();
        services.AddHttpClient(TokenProvider.NombreCliente, c => c.BaseAddress = new Uri("http://auth.test"))
            .ConfigurePrimaryHttpMessageHandler(() => servidor);
        var configuracion = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:ClientId"] = "asistente-service",
            ["Authentication:ClientSecret"] = "secreto-de-prueba"
        }).Build();

        var proveedor = new TokenProvider(services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>(), configuracion, reloj);
        return (proveedor, servidor, reloj);
    }

    [Fact]
    public async Task El_token_se_reutiliza_hasta_un_minuto_antes_de_vencer()
    {
        var (proveedor, servidor, reloj) = CrearTokenProvider(new DateTimeOffset(2026, 10, 22, 13, 0, 0, TimeSpan.Zero));

        var primero = await proveedor.ObtenerAsync();
        reloj.Ahora = new DateTimeOffset(2026, 10, 22, 12, 58, 0, TimeSpan.Zero); // faltan 2 min: sigue valiendo
        var segundo = await proveedor.ObtenerAsync();

        Assert.Equal(primero.AccessToken, segundo.AccessToken);
        Assert.Single(servidor.Pedidos);
    }

    [Fact]
    public async Task Pasado_el_margen_de_un_minuto_pide_un_token_nuevo()
    {
        var (proveedor, servidor, reloj) = CrearTokenProvider(new DateTimeOffset(2026, 10, 22, 13, 0, 0, TimeSpan.Zero));

        var primero = await proveedor.ObtenerAsync();
        reloj.Ahora = new DateTimeOffset(2026, 10, 22, 12, 59, 30, TimeSpan.Zero); // faltan 30 s: ya hay que renovar
        var segundo = await proveedor.ObtenerAsync();

        Assert.NotEqual(primero.AccessToken, segundo.AccessToken);
        Assert.Equal(2, servidor.Pedidos.Count);
    }

    [Fact]
    public async Task El_token_no_se_pide_hasta_que_alguien_lo_necesita()
    {
        var (_, servidor, _) = CrearTokenProvider(new DateTimeOffset(2026, 10, 22, 13, 0, 0, TimeSpan.Zero));

        Assert.Empty(servidor.Pedidos);
        await Task.CompletedTask;
    }
}
