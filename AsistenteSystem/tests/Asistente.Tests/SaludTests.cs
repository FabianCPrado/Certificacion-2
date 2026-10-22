using Asistente.Infrastructure.Servicios;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Asistente.Tests;

internal sealed class TokenQueFalla : ITokenProvider
{
    public Task<TokenInfo> ObtenerAsync(bool renovar = false, CancellationToken cancellationToken = default) =>
        throw new HttpRequestException("Authentication no responde.");
}

public class SaludTests
{
    private static (VerificadorSalud Verificador, ServidorFalso Catalogo) Crear(ITokenProvider token)
    {
        var catalogo = new ServidorFalso(_ => ServidorFalso.Json("""["Abarrotes","Bebidas"]"""));
        var pedidos = new ServidorFalso(_ => ServidorFalso.Json("""{"items":[],"pagina":1,"tamanio":1,"total":7}"""));
        var clientes = new ServidorFalso(_ => ServidorFalso.Json("""{"items":[],"pagina":1,"tamanio":1,"total":3}"""));

        var services = new ServiceCollection();
        services.AddHttpClient(VerificadorSalud.Catalogo, c => c.BaseAddress = new Uri("http://catalogo.test"))
            .ConfigurePrimaryHttpMessageHandler(() => catalogo);
        services.AddHttpClient(VerificadorSalud.Pedidos, c => c.BaseAddress = new Uri("http://pedidos.test"))
            .ConfigurePrimaryHttpMessageHandler(() => pedidos);
        services.AddHttpClient(VerificadorSalud.Clientes, c => c.BaseAddress = new Uri("http://clientes.test"))
            .ConfigurePrimaryHttpMessageHandler(() => clientes);

        var verificador = new VerificadorSalud(
            services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>(), token,
            new MapaFalso(new Asistente.Domain.HerramientaInfo("ObtenerPrecioYStock", "d", "Catalogo", "GET /x", true)),
            new ConfigurationBuilder().Build());
        return (verificador, catalogo);
    }

    [Fact]
    public async Task Si_Authentication_falla_Catalogo_dice_que_no_hay_JWT_y_no_se_le_consulta()
    {
        var (verificador, catalogo) = Crear(new TokenQueFalla());

        var salud = await verificador.VerificarAsync();

        var componente = salud.Componentes.Single(c => c.Nombre == "Catalogo");
        Assert.Equal("Error", componente.Estado);
        Assert.Contains("Sin JWT", componente.Detalle);
        Assert.Empty(catalogo.Pedidos);
        Assert.Equal("Degradado", salud.Estado);
    }

    [Fact]
    public async Task Con_token_valido_Catalogo_Pedidos_y_Clientes_estan_Ok()
    {
        var (verificador, _) = Crear(new TokenFalso());

        var salud = await verificador.VerificarAsync();

        Assert.All(
            salud.Componentes.Where(c => c.Nombre is "Authentication" or "Catalogo" or "Pedidos" or "Clientes"),
            c => Assert.Equal("Ok", c.Estado));
        Assert.Contains("2 categorías", salud.Componentes.Single(c => c.Nombre == "Catalogo").Detalle);
        Assert.Contains("7 pedidos", salud.Componentes.Single(c => c.Nombre == "Pedidos").Detalle);
    }
}
