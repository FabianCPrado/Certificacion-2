using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Asistente.Application.Asistente;
using Asistente.Application.Exceptions;
using Asistente.Application.Interfaces;

namespace Asistente.Infrastructure.Servicios;

/// <summary>Lectura de JSON de otro servicio: 404 → null; cualquier otro fallo → ServicioNoDisponibleException.</summary>
internal static class LecturaHttp
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static async Task<T?> LeerAsync<T>(HttpClient http, string servicio, string ruta, CancellationToken cancellationToken)
        where T : class
    {
        try
        {
            using var respuesta = await http.GetAsync(ruta, cancellationToken);
            if (respuesta.StatusCode == HttpStatusCode.NotFound)
                return null;
            if (!respuesta.IsSuccessStatusCode)
                throw new ServicioNoDisponibleException($"{servicio} respondió HTTP {(int)respuesta.StatusCode}.");

            return await respuesta.Content.ReadFromJsonAsync<T>(Json, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new ServicioNoDisponibleException($"{servicio} no responde.", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ServicioNoDisponibleException($"{servicio} tardó demasiado en responder.", ex);
        }
    }
}

public sealed class CatalogoConsultasHttp(IHttpClientFactory httpClientFactory) : ICatalogoConsultas
{
    private HttpClient Http => httpClientFactory.CreateClient(VerificadorSalud.Catalogo);

    public Task<ProductoInfo?> ObtenerAsync(string categoria, string id, CancellationToken cancellationToken = default) =>
        LecturaHttp.LeerAsync<ProductoInfo>(
            Http, "Catálogo", $"/api/productos/{Uri.EscapeDataString(categoria)}/{Uri.EscapeDataString(id)}", cancellationToken);

    private const int TamanioPagina = 100;
    private const int MaxPaginas = 10;

    // El catálogo busca con LIKE y distingue tildes, y exige una categoría: aquí se trae por /todos y se filtra sin
    // importar tildes ni mayúsculas, así "café" encuentra "Cafe molido" y no hace falta adivinar la categoría.
    public async Task<IReadOnlyList<ProductoInfo>> BuscarAsync(string? categoria, string nombre, CancellationToken cancellationToken = default)
    {
        var encontrados = new List<ProductoInfo>();

        for (var pagina = 1; pagina <= MaxPaginas; pagina++)
        {
            var productos = await LecturaHttp.LeerAsync<List<ProductoInfo>>(
                Http, "Catálogo", $"/api/productos/todos?pagina={pagina}&tamanio={TamanioPagina}", cancellationToken) ?? [];

            encontrados.AddRange(productos.Where(p =>
                (string.IsNullOrWhiteSpace(categoria) || Iguales(p.Categoria, categoria)) && Contiene(p.Nombre, nombre)));

            if (productos.Count < TamanioPagina)
                break;
        }

        return encontrados;
    }

    private static bool Iguales(string a, string b) =>
        string.Compare(a.Trim(), b.Trim(), CultureInfo.InvariantCulture, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) == 0;

    private static bool Contiene(string texto, string buscado) =>
        CultureInfo.InvariantCulture.CompareInfo.IndexOf(
            texto, buscado.Trim(), CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0;
}

public sealed class PedidosConsultasHttp(IHttpClientFactory httpClientFactory) : IPedidosConsultas
{
    private HttpClient Http => httpClientFactory.CreateClient(VerificadorSalud.Pedidos);

    // Pedidos devuelve { items: [...] }; el endpoint /detalle ya trae las líneas y no trae el email
    public async Task<IReadOnlyList<PedidoInfo>> UltimosDeClienteAsync(Guid clienteId, int cantidad, CancellationToken cancellationToken = default)
    {
        var pagina = await LecturaHttp.LeerAsync<Pagina>(
            Http, "Pedidos", $"/api/pedidos/detalle?clienteId={clienteId}&tamanio={Math.Clamp(cantidad, 1, 20)}", cancellationToken);
        return pagina?.Items ?? [];
    }

    public Task<PedidoInfo?> ObtenerAsync(Guid pedidoId, CancellationToken cancellationToken = default) =>
        LecturaHttp.LeerAsync<PedidoInfo>(Http, "Pedidos", $"/api/pedidos/{pedidoId}", cancellationToken);

    private sealed record Pagina(List<PedidoInfo> Items);
}

public sealed class ClientesConsultasHttp(IHttpClientFactory httpClientFactory) : IClientesConsultas
{
    // La API devuelve también el email, pero ClienteInfo no lo declara: nunca se materializa
    public Task<ClienteInfo?> ObtenerAsync(Guid clienteId, CancellationToken cancellationToken = default) =>
        LecturaHttp.LeerAsync<ClienteInfo>(
            httpClientFactory.CreateClient(VerificadorSalud.Clientes), "Clientes", $"/api/clientes/{clienteId}", cancellationToken);
}
