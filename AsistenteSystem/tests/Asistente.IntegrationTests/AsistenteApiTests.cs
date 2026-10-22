using System.Net;
using System.Net.Http.Json;
using Asistente.Domain;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Asistente.IntegrationTests;

public class AsistenteApiTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    // Sin clave de IA: los tests nunca llaman a Claude (ni gastan), aunque el equipo tenga IA:ApiKey en User Secrets
    private readonly HttpClient _client = factory
        .WithWebHostBuilder(b => b.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?> { ["IA:ApiKey"] = "" })))
        .CreateClient();

    [Fact]
    public async Task Herramientas_devuelve_el_mapa()
    {
        var herramientas = await _client.GetFromJsonAsync<HerramientaInfo[]>("/api/asistente/herramientas");

        Assert.Equal(5, herramientas!.Length);
    }

    [Fact]
    public async Task Preguntar_con_datos_invalidos_devuelve_400_con_ProblemDetails()
    {
        var respuesta = await _client.PostAsJsonAsync("/api/asistente/preguntar", new { pregunta = "", clienteId = (Guid?)null });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
    }

    // --- Apartado Sistemas ---------------------------------------------------------------------------------------------

    [Fact]
    public async Task Sistemas_lista_los_cinco_sistemas()
    {
        var sistemas = await _client.GetFromJsonAsync<System.Text.Json.JsonElement>("/api/sistemas");

        Assert.Equal(5, sistemas.GetArrayLength());
    }

    [Theory]
    [InlineData("pedidos")]
    [InlineData("PedidosSystem")]
    public async Task Sistema_por_nombre_devuelve_su_estructura(string nombre)
    {
        var sistema = await _client.GetFromJsonAsync<System.Text.Json.JsonElement>($"/api/sistemas/{nombre}");

        Assert.Equal("Pedidos", sistema.GetProperty("nombre").GetString());
        Assert.True(sistema.GetProperty("endpoints").GetArrayLength() > 5);
        Assert.True(sistema.GetProperty("capas").GetArrayLength() == 4);
    }

    [Fact]
    public async Task Sistema_inexistente_devuelve_404_con_los_nombres_validos()
    {
        var respuesta = await _client.GetAsync("/api/sistemas/facturas");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Contains("Catalogo", await respuesta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Impacto_de_Catalogo_incluye_a_Pedidos_y_al_Asistente()
    {
        var impacto = await _client.GetFromJsonAsync<System.Text.Json.JsonElement>("/api/sistemas/catalogo/impacto");

        var consumidores = impacto.GetProperty("loConsumen").EnumerateArray().Select(c => c.GetProperty("sistema").GetString()).ToList();
        Assert.Contains("Pedidos", consumidores);
        Assert.Contains("Asistente", consumidores);
    }

    [Fact]
    public async Task Buscar_encuentra_el_endpoint_de_detalle_de_pedidos()
    {
        var busqueda = await _client.GetFromJsonAsync<System.Text.Json.JsonElement>("/api/sistemas/buscar?texto=detalle");

        Assert.True(busqueda.GetProperty("total").GetInt32() > 0);
        Assert.Contains(busqueda.GetProperty("resultados").EnumerateArray(),
            r => r.GetProperty("detalle").GetString()!.Contains("/api/pedidos/detalle"));
    }

    [Fact]
    public async Task Buscar_con_un_texto_demasiado_corto_devuelve_400()
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.GetAsync("/api/sistemas/buscar?texto=a")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.GetAsync("/api/sistemas/buscar")).StatusCode);
    }

    [Theory]
    [InlineData("arquitectura", 7)]
    [InlineData("instrucciones", 6)]
    public async Task Lista_los_documentos_de_la_base_de_conocimiento(string tipo, int cantidad)
    {
        var documentos = await _client.GetFromJsonAsync<System.Text.Json.JsonElement>($"/api/sistemas/{tipo}");

        Assert.Equal(cantidad, documentos.GetArrayLength());
    }

    [Theory]
    [InlineData("arquitectura", "capas-y-dependencias")]
    [InlineData("instrucciones", "agregar-un-endpoint")]
    public async Task Devuelve_un_documento_completo_en_markdown(string tipo, string id)
    {
        var documento = await _client.GetFromJsonAsync<System.Text.Json.JsonElement>($"/api/sistemas/{tipo}/{id}");

        Assert.StartsWith("# ", documento.GetProperty("contenido").GetString());
    }

    [Fact]
    public async Task Un_documento_inexistente_devuelve_404()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/api/sistemas/instrucciones/no-existe")).StatusCode);
    }

    [Fact]
    public async Task Preguntar_con_un_json_mal_formado_devuelve_400()
    {
        using var cuerpo = new StringContent("{ esto no es json", System.Text.Encoding.UTF8, "application/json");

        var respuesta = await _client.PostAsync("/api/asistente/preguntar", cuerpo);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task Preguntar_sin_modelo_configurado_devuelve_503()
    {
        var respuesta = await _client.PostAsJsonAsync("/api/asistente/preguntar", new { pregunta = "hay stock de yerba?", clienteId = (Guid?)null });

        Assert.Equal(HttpStatusCode.ServiceUnavailable, respuesta.StatusCode);
        Assert.Contains("no está configurado", await respuesta.Content.ReadAsStringAsync());
    }
}
