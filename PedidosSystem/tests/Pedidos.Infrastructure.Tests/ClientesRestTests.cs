using System.Net;
using System.Text;
using Pedidos.Application.Exceptions;
using Pedidos.Infrastructure.Rest.Clientes;

namespace Pedidos.Infrastructure.Tests;

/// <summary>La consulta REST contra un servicio Clientes simulado: no abre red.</summary>
public class ClientesRestTests
{
    private sealed class RespuestaFija(HttpStatusCode estado, string cuerpo = "") : HttpMessageHandler
    {
        public HttpRequestMessage? Recibida { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Recibida = request;
            return Task.FromResult(new HttpResponseMessage(estado)
            {
                Content = new StringContent(cuerpo, Encoding.UTF8, "application/json")
            });
        }
    }

    private static ClientesRestConsultas Consultas(HttpMessageHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("http://clientes.test/") });

    [Fact]
    public async Task Obtener_devuelve_el_cliente_del_servicio()
    {
        var id = Guid.NewGuid();
        var handler = new RespuestaFija(HttpStatusCode.OK, $$"""{"id":"{{id}}","nombre":"Ana","email":"ana@ejemplo.com"}""");

        var cliente = await Consultas(handler).ObtenerPorIdAsync(id);

        Assert.NotNull(cliente);
        Assert.Equal(id, cliente.Id);
        Assert.Equal("Ana", cliente.Nombre);
        Assert.Equal(HttpMethod.Get, handler.Recibida!.Method);
    }

    [Fact]
    public async Task Obtener_un_cliente_inexistente_devuelve_null()
    {
        Assert.Null(await Consultas(new RespuestaFija(HttpStatusCode.NotFound)).ObtenerPorIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Listar_deserializa_la_pagina_del_servicio()
    {
        var handler = new RespuestaFija(HttpStatusCode.OK,
            $$"""{"items":[{"id":"{{Guid.NewGuid()}}","nombre":"Ana","email":"ana@ejemplo.com"}],"pagina":1,"tamanio":20,"total":1}""");

        var pagina = await Consultas(handler).ListarAsync(2, 20);

        Assert.Equal(1, pagina.Total);
        Assert.Equal("Ana", Assert.Single(pagina.Items).Nombre);
        Assert.Equal("/api/clientes", handler.Recibida!.RequestUri!.AbsolutePath);
        Assert.Equal("?pagina=2&tamanio=20", handler.Recibida.RequestUri.Query);
    }

    [Fact]
    public async Task Un_404_al_listar_se_convierte_en_NotFoundException()
    {
        var consultas = Consultas(new RespuestaFija(HttpStatusCode.NotFound, """{"detail":"No existe."}"""));

        await Assert.ThrowsAsync<NotFoundException>(() => consultas.ListarAsync(1, 20));
    }

    [Fact]
    public async Task Un_error_del_servidor_se_propaga_como_HttpRequestException()
    {
        var consultas = Consultas(new RespuestaFija(HttpStatusCode.InternalServerError));

        await Assert.ThrowsAsync<HttpRequestException>(() => consultas.ListarAsync(1, 20));
    }
}
