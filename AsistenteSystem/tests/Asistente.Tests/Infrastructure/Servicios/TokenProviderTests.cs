using Asistente.Infrastructure.Servicios;
using Asistente.Tests.Infrastructure.Fakes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Asistente.Tests.Infrastructure.Servicios;

public class TokenProviderTests
{
    private static (TokenProvider Proveedor, ServidorFalso Servidor, RelojFalso Reloj) Crear(DateTimeOffset vence)
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
        var (proveedor, servidor, reloj) = Crear(new DateTimeOffset(2026, 10, 22, 13, 0, 0, TimeSpan.Zero));

        var primero = await proveedor.ObtenerAsync();
        reloj.Ahora = new DateTimeOffset(2026, 10, 22, 12, 58, 0, TimeSpan.Zero); // faltan 2 min: sigue valiendo
        var segundo = await proveedor.ObtenerAsync();

        Assert.Equal(primero.AccessToken, segundo.AccessToken);
        Assert.Single(servidor.Pedidos);
    }

    [Fact]
    public async Task Pasado_el_margen_de_un_minuto_pide_un_token_nuevo()
    {
        var (proveedor, servidor, reloj) = Crear(new DateTimeOffset(2026, 10, 22, 13, 0, 0, TimeSpan.Zero));

        var primero = await proveedor.ObtenerAsync();
        reloj.Ahora = new DateTimeOffset(2026, 10, 22, 12, 59, 30, TimeSpan.Zero); // faltan 30 s: ya hay que renovar
        var segundo = await proveedor.ObtenerAsync();

        Assert.NotEqual(primero.AccessToken, segundo.AccessToken);
        Assert.Equal(2, servidor.Pedidos.Count);
    }

    [Fact]
    public async Task El_token_no_se_pide_hasta_que_alguien_lo_necesita()
    {
        var (_, servidor, _) = Crear(new DateTimeOffset(2026, 10, 22, 13, 0, 0, TimeSpan.Zero));

        Assert.Empty(servidor.Pedidos);
        await Task.CompletedTask;
    }
}
