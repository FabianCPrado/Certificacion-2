using Pedidos.Application.Interfaces.Common;
using Pedidos.Application.Exceptions;
using Pedidos.Application.Interfaces.Pedidos;
using Pedidos.Application.Interfaces.Productos;
using Pedidos.Application.Productos.Queries.ObtenerProducto;
using Pedidos.Domain.Common;
using Pedidos.Application.Pedidos.Commands.CrearPedido;
using Pedidos.Domain.Pedidos;
using Pedidos.Domain.ValueObjects;

namespace Pedidos.Application.Tests;

internal sealed class FakePedidoRepository : IPedidoRepository
{
    public List<Pedido> Guardados { get; } = [];

    public Task<Pedido?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Guardados.Find(p => p.Id == id));

    public Pedido Crear(Guid clienteId, Email emailCliente, Direccion direccionEnvio, string moneda, DateTime creadoEn)
    {
        var pedido = new Pedido
        {
            ClienteId = clienteId,
            EmailCliente = emailCliente,
            DireccionEnvio = direccionEnvio,
            Moneda = moneda,
            CreadoEn = creadoEn
        };
        Guardados.Add(pedido);
        return pedido;
    }

    public int VecesGuardado { get; private set; }

    public Task<Pedido> CrearAsync(
        Guid clienteId, Email emailCliente, Direccion direccionEnvio, string moneda, DateTime creadoEn,
        IReadOnlyList<LineaNueva> lineas, CancellationToken cancellationToken = default)
    {
        var pedido = Crear(clienteId, emailCliente, direccionEnvio, moneda, creadoEn);
        foreach (var l in lineas)
            AgregarLinea(pedido, l.ProductoId, l.Categoria, l.NombreProducto, l.PrecioUnitario, l.Cantidad);
        VecesGuardado++;
        return Task.FromResult(pedido);
    }

    public void AgregarLinea(Pedido pedido, string productoId, string categoria, string nombreProducto, Dinero precioUnitario, int cantidad) =>
        pedido.Lineas.Add(new LineaPedido
        {
            ProductoId = productoId,
            Categoria = categoria,
            NombreProducto = nombreProducto,
            PrecioUnitario = precioUnitario,
            Cantidad = cantidad
        });

    public void Confirmar(Pedido pedido) => pedido.Estado = EstadoPedido.Confirmado;

    public void Cancelar(Pedido pedido) => pedido.Estado = EstadoPedido.Cancelado;

    public Task ConfirmarAsync(Pedido pedido, CancellationToken cancellationToken = default)
    {
        Confirmar(pedido);
        VecesGuardado++;
        return Task.CompletedTask;
    }

    public Task CancelarAsync(Pedido pedido, CancellationToken cancellationToken = default)
    {
        Cancelar(pedido);
        VecesGuardado++;
        return Task.CompletedTask;
    }

    public Task ActualizarAsync(
        Pedido pedido, Email emailCliente, Direccion direccionEnvio, CancellationToken cancellationToken = default)
    {
        pedido.EmailCliente = emailCliente;
        pedido.DireccionEnvio = direccionEnvio;
        VecesGuardado++;
        return Task.CompletedTask;
    }

    public Task<int> CancelarBorradoresAntiguosAsync(int dias, CancellationToken cancellationToken = default)
    {
        VecesGuardado++;
        return Task.FromResult(Guardados.Count(p => p.Estado == EstadoPedido.Borrador));
    }

    public Task EliminarAsync(Pedido pedido, CancellationToken cancellationToken = default)
    {
        Guardados.Remove(pedido);
        VecesGuardado++;
        return Task.CompletedTask;
    }
}

internal sealed class FakeAppDbContext : IAppDbContext
{
    public int VecesGuardado { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        VecesGuardado++;
        return Task.FromResult(1);
    }
}

internal static class Comandos
{
    public static CrearPedidoCommand CrearPedidoValido(string email = "ana@ejemplo.com") => new(
        ClienteId: Guid.NewGuid(),
        EmailCliente: email,
        Direccion: new DireccionRequest("Calle 100 # 15-20", "Bogotá", "110111", "Colombia"),
        Moneda: "COP",
        Lineas: [new LineaRequest("p-1", "Electro", 2)]);
}

/// <summary>Catálogo en memoria: cualquier producto existe con precio 150.000, salvo los marcados como inexistentes.</summary>
internal sealed class FakeCatalogoProductos : ICatalogoProductos
{
    public HashSet<string> Inexistentes { get; } = [];

    public Dictionary<string, int> Stock { get; } = [];

    public int Descuentos { get; private set; }

    public int Reposiciones { get; private set; }

    public Task<ProductoDto?> ObtenerAsync(
        string categoria, string id, CancellationToken cancellationToken = default) =>
        Task.FromResult<ProductoDto?>(
            Inexistentes.Contains(id) ? null : new(id, categoria, "Teclado", 150_000m, 100));

    public Task DescontarStockAsync(string categoria, string id, int cantidad, CancellationToken cancellationToken = default)
    {
        if (Inexistentes.Contains(id))
            throw new NotFoundException($"No existe el producto {id}.");

        if (Stock.TryGetValue(id, out var disponible) && disponible < cantidad)
            throw new ReglaNegocioException($"No hay stock suficiente del producto {id}.");

        Descuentos++;
        return Task.CompletedTask;
    }

    public Task ReponerStockAsync(string categoria, string id, int cantidad, CancellationToken cancellationToken = default)
    {
        Reposiciones++;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ProductoDto>> ListarAsync(
        string categoria, int pagina, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<ProductoDto> CrearAsync(
        string categoria, string nombre, decimal precio, int stock, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task ActualizarAsync(
        string categoria, string id, string nombre, decimal precio, int stock, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task CambiarPrecioAsync(string categoria, string id, decimal precio, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task EliminarAsync(string categoria, string id, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<int> AumentarPrecioCategoriaAsync(string categoria, decimal porcentaje, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}
