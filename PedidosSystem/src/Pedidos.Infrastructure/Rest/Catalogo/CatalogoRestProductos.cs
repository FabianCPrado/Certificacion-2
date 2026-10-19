using System.Net;
using System.Net.Http.Json;
using Pedidos.Application.Interfaces.Productos;
using Pedidos.Application.Productos.Queries.ObtenerProducto;
using Pedidos.Infrastructure.Rest.Clientes;

namespace Pedidos.Infrastructure.Rest.Catalogo;

/// <summary>
/// Productos contra el servicio Catálogo (CatalogoSystem) por REST. Application usa <see cref="ICatalogoProductos"/>
/// y no sabe que hay HTTP detrás. El id y la categoría viajan en la ruta, tal como los define el Catálogo.
/// </summary>
public sealed class CatalogoRestProductos(HttpClient http) : ICatalogoProductos
{
    private const string Ruta = "api/productos";
    private const string Servicio = "Catálogo";

    private sealed record CrearRequest(string Categoria, string Nombre, decimal Precio, int Stock);

    private sealed record ActualizarRequest(string Nombre, decimal Precio, int Stock);

    private sealed record PrecioRequest(decimal Precio);

    private sealed record StockRequest(int Cantidad);

    private sealed record PorcentajeRequest(decimal Porcentaje);

    private static string Producto(string categoria, string id) =>
        $"{Ruta}/{Uri.EscapeDataString(categoria)}/{Uri.EscapeDataString(id)}";

    public async Task<ProductoDto?> ObtenerAsync(string categoria, string id, CancellationToken cancellationToken = default)
    {
        using var respuesta = await http.GetAsync(Producto(categoria, id), cancellationToken);

        if (respuesta.StatusCode == HttpStatusCode.NotFound)
            return null;

        await ClientesRestRespuestas.AsegurarExitoAsync(respuesta, cancellationToken, Servicio);
        return await respuesta.Content.ReadFromJsonAsync<ProductoDto>(cancellationToken);
    }

    public async Task<IReadOnlyList<ProductoDto>> ListarAsync(
        string categoria, int pagina, CancellationToken cancellationToken = default)
    {
        using var respuesta = await http.GetAsync(
            $"{Ruta}?categoria={Uri.EscapeDataString(categoria)}&pagina={pagina}", cancellationToken);
        await ClientesRestRespuestas.AsegurarExitoAsync(respuesta, cancellationToken, Servicio);

        return await respuesta.Content.ReadFromJsonAsync<List<ProductoDto>>(cancellationToken)
            ?? throw new HttpRequestException("El servicio Catálogo devolvió una respuesta vacía.");
    }

    public async Task<ProductoDto> CrearAsync(
        string categoria, string nombre, decimal precio, int stock, CancellationToken cancellationToken = default)
    {
        using var respuesta = await http.PostAsJsonAsync(
            Ruta, new CrearRequest(categoria, nombre, precio, stock), cancellationToken);
        await ClientesRestRespuestas.AsegurarExitoAsync(respuesta, cancellationToken, Servicio);

        return await respuesta.Content.ReadFromJsonAsync<ProductoDto>(cancellationToken)
            ?? throw new HttpRequestException("El servicio Catálogo devolvió una respuesta vacía.");
    }

    public async Task ActualizarAsync(
        string categoria, string id, string nombre, decimal precio, int stock, CancellationToken cancellationToken = default)
    {
        using var respuesta = await http.PutAsJsonAsync(
            Producto(categoria, id), new ActualizarRequest(nombre, precio, stock), cancellationToken);
        await ClientesRestRespuestas.AsegurarExitoAsync(respuesta, cancellationToken, Servicio);
    }

    public async Task CambiarPrecioAsync(string categoria, string id, decimal precio, CancellationToken cancellationToken = default)
    {
        using var respuesta = await http.PutAsJsonAsync(
            $"{Producto(categoria, id)}/precio", new PrecioRequest(precio), cancellationToken);
        await ClientesRestRespuestas.AsegurarExitoAsync(respuesta, cancellationToken, Servicio);
    }

    public async Task EliminarAsync(string categoria, string id, CancellationToken cancellationToken = default)
    {
        using var respuesta = await http.DeleteAsync(Producto(categoria, id), cancellationToken);
        await ClientesRestRespuestas.AsegurarExitoAsync(respuesta, cancellationToken, Servicio);
    }

    public async Task<int> AumentarPrecioCategoriaAsync(
        string categoria, decimal porcentaje, CancellationToken cancellationToken = default)
    {
        using var respuesta = await http.PostAsJsonAsync(
            $"{Ruta}/{Uri.EscapeDataString(categoria)}/aumentar-precio", new PorcentajeRequest(porcentaje), cancellationToken);
        await ClientesRestRespuestas.AsegurarExitoAsync(respuesta, cancellationToken, Servicio);

        return await respuesta.Content.ReadFromJsonAsync<int>(cancellationToken);
    }

    public async Task DescontarStockAsync(string categoria, string id, int cantidad, CancellationToken cancellationToken = default)
    {
        using var respuesta = await http.PostAsJsonAsync(
            $"{Producto(categoria, id)}/stock/descontar", new StockRequest(cantidad), cancellationToken);
        await ClientesRestRespuestas.AsegurarExitoAsync(respuesta, cancellationToken, Servicio);
    }

    public async Task ReponerStockAsync(string categoria, string id, int cantidad, CancellationToken cancellationToken = default)
    {
        using var respuesta = await http.PostAsJsonAsync(
            $"{Producto(categoria, id)}/stock/reponer", new StockRequest(cantidad), cancellationToken);
        await ClientesRestRespuestas.AsegurarExitoAsync(respuesta, cancellationToken, Servicio);
    }
}
