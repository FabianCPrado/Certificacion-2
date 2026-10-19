using System.Net;
using System.Net.Http.Json;
using Pedidos.Application.Clientes.Queries.ObtenerCliente;
using Pedidos.Application.Common;
using Pedidos.Application.Interfaces.Clientes;

namespace Pedidos.Infrastructure.Rest.Clientes;

/// <summary>Lectura de clientes contra el servicio Clientes por REST. Reemplaza a las consultas con EF Core.</summary>
public sealed class ClientesRestConsultas(HttpClient http) : IClienteConsultas
{
    private const string Ruta = "api/clientes";

    public async Task<ClienteDto?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var respuesta = await http.GetAsync($"{Ruta}/{id}", cancellationToken);

        if (respuesta.StatusCode == HttpStatusCode.NotFound)
            return null;

        await ClientesRestRespuestas.AsegurarExitoAsync(respuesta, cancellationToken);

        return await respuesta.Content.ReadFromJsonAsync<ClienteDto>(cancellationToken);
    }

    public async Task<PagedResult<ClienteDto>> ListarAsync(
        int pagina, int tamanio, CancellationToken cancellationToken = default)
    {
        using var respuesta = await http.GetAsync($"{Ruta}?pagina={pagina}&tamanio={tamanio}", cancellationToken);
        await ClientesRestRespuestas.AsegurarExitoAsync(respuesta, cancellationToken);

        return await respuesta.Content.ReadFromJsonAsync<PagedResult<ClienteDto>>(cancellationToken)
            ?? throw new HttpRequestException("El servicio Clientes devolvió una respuesta vacía.");
    }
}
