using System.Net.Http.Json;
using System.Text.Json;
using Clientes.Application.Interfaces.Pedidos;

namespace Clientes.Infrastructure.Rest.Pedidos;

/// <summary>Consulta al servicio Pedidos por REST: <c>GET api/pedidos?clienteId=…&amp;tamanio=1</c> y lee el total.</summary>
public sealed class PedidosRestConsultas(HttpClient http) : IPedidosConsultas
{
    public async Task<bool> ClienteTienePedidosAsync(Guid clienteId, CancellationToken cancellationToken = default)
    {
        using var respuesta = await http.GetAsync(
            $"api/pedidos?clienteId={clienteId}&pagina=1&tamanio=1", cancellationToken);

        if (!respuesta.IsSuccessStatusCode)
            throw new HttpRequestException($"El servicio Pedidos respondió {(int)respuesta.StatusCode}.");

        await using var cuerpo = await respuesta.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(cuerpo, cancellationToken: cancellationToken);

        if (!json.RootElement.TryGetProperty("total", out var total) || !total.TryGetInt32(out var cantidad))
            throw new HttpRequestException("El servicio Pedidos devolvió una respuesta inesperada.");

        return cantidad > 0;
    }

    /// <summary><c>GET api/cache/pedidos?desde=…&amp;hasta=…</c>: el caché Redis vive en Pedidos, no aquí.</summary>
    public async Task<PedidosPorFechasDto> ListarPorFechasAsync(
        DateOnly desde, DateOnly hasta, CancellationToken cancellationToken = default)
    {
        using var respuesta = await http.GetAsync(
            $"api/cache/pedidos?desde={desde:yyyy-MM-dd}&hasta={hasta:yyyy-MM-dd}", cancellationToken);

        if (!respuesta.IsSuccessStatusCode)
            throw new HttpRequestException($"El servicio Pedidos respondió {(int)respuesta.StatusCode}.");

        return await respuesta.Content.ReadFromJsonAsync<PedidosPorFechasDto>(cancellationToken)
            ?? throw new HttpRequestException("El servicio Pedidos devolvió una respuesta inesperada.");
    }
}
