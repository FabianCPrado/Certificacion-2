using System.Net;
using Asistente.Application.Asistente;
using Asistente.Application.Exceptions;
using Asistente.Infrastructure.Servicios;
using Asistente.Tests.Infrastructure.Fakes;
using Microsoft.Extensions.DependencyInjection;

namespace Asistente.Tests.Infrastructure.Servicios;

public class ConsultasHttpTests
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
        Assert.Equal(new ProductoInfo("Abarrotes", "y1", "Yerba mate 1 kg", 4.20m, 12), producto);
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
}
