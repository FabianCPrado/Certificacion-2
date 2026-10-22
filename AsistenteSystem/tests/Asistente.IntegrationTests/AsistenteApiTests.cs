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

    // --- Apartado Sistemas: un solo endpoint donde se le pregunta a la IA ----------------------------------------------

    [Fact]
    public async Task Sistemas_preguntar_con_datos_invalidos_devuelve_400()
    {
        var respuesta = await _client.PostAsJsonAsync("/api/sistemas/preguntar", new { pregunta = "" });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Sistemas_preguntar_sin_modelo_configurado_devuelve_503_y_no_llama_a_Claude()
    {
        var respuesta = await _client.PostAsJsonAsync("/api/sistemas/preguntar", new { pregunta = "¿Qué sistemas hay?" });

        Assert.Equal(HttpStatusCode.ServiceUnavailable, respuesta.StatusCode);
        Assert.Contains("no está configurado", await respuesta.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("/api/sistemas")]
    [InlineData("/api/sistemas/pedidos")]
    [InlineData("/api/sistemas/buscar?texto=stock")]
    [InlineData("/api/sistemas/arquitectura")]
    public async Task Sistemas_tiene_un_solo_endpoint_los_demas_no_existen(string ruta) =>
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync(ruta)).StatusCode);

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
