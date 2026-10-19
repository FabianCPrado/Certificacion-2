using System.Net;
using System.Text;
using Clientes.Application.Interfaces.Pedidos;
using Clientes.Domain.Clientes;
using Clientes.Domain.Common;
using Clientes.Infrastructure.Persistence;
using Clientes.Infrastructure.Persistence.Repositories.Clientes;
using Clientes.Infrastructure.Rest.Pedidos;
using Microsoft.EntityFrameworkCore;

namespace Clientes.Infrastructure.Tests;

/// <summary>La regla "no eliminar un cliente con pedidos", con el servicio Pedidos simulado: no abre red ni base de datos.</summary>
public class PedidosRestTests
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

    private sealed class PedidosFalsos(bool tienePedidos) : IPedidosConsultas
    {
        public Task<bool> ClienteTienePedidosAsync(Guid clienteId, CancellationToken cancellationToken = default) =>
            Task.FromResult(tienePedidos);

        public Task<PedidosPorFechasDto> ListarPorFechasAsync(DateOnly desde, DateOnly hasta, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private static PedidosRestConsultas Consultas(HttpMessageHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("http://pedidos.test/") });

    private static string Pagina(int total) =>
        $$"""{"items":[],"pagina":1,"tamanio":1,"total":{{total}}}""";

    [Fact]
    public async Task Con_pedidos_devuelve_true_y_filtra_por_cliente()
    {
        var clienteId = Guid.NewGuid();
        var handler = new RespuestaFija(HttpStatusCode.OK, Pagina(3));

        Assert.True(await Consultas(handler).ClienteTienePedidosAsync(clienteId));
        Assert.Contains($"clienteId={clienteId}", handler.Recibida!.RequestUri!.Query);
    }

    [Fact]
    public async Task Sin_pedidos_devuelve_false() =>
        Assert.False(await Consultas(new RespuestaFija(HttpStatusCode.OK, Pagina(0))).ClienteTienePedidosAsync(Guid.NewGuid()));

    [Fact]
    public async Task Un_error_de_Pedidos_se_propaga_como_HttpRequestException() =>
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            Consultas(new RespuestaFija(HttpStatusCode.InternalServerError)).ClienteTienePedidosAsync(Guid.NewGuid()));

    [Fact]
    public async Task Una_respuesta_sin_total_se_trata_como_error() =>
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            Consultas(new RespuestaFija(HttpStatusCode.OK, "{}")).ClienteTienePedidosAsync(Guid.NewGuid()));

    [Fact]
    public async Task Eliminar_un_cliente_con_pedidos_lanza_ReglaNegocioException()
    {
        var repositorio = new ClienteRepository(
            new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer("Server=.;Database=Pruebas").Options),
            new PedidosFalsos(tienePedidos: true));

        var error = await Assert.ThrowsAsync<ReglaNegocioException>(() => repositorio.EliminarAsync(new Cliente()));

        Assert.Equal("No se puede eliminar un cliente que tiene pedidos.", error.Message);
    }
}
