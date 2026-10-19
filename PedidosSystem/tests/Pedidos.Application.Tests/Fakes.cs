using Pedidos.Application.Interfaces.Common;
using Pedidos.Application.Interfaces.Pedidos;
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
            AgregarLinea(pedido, l.ProductoId, l.NombreProducto, l.PrecioUnitario, l.Cantidad);
        VecesGuardado++;
        return Task.FromResult(pedido);
    }

    public Task<Pedido> CrearYDescontarStockAsync(
        Guid clienteId, Email emailCliente, Direccion direccionEnvio, string moneda, DateTime creadoEn,
        IReadOnlyList<LineaNueva> lineas, CancellationToken cancellationToken = default) =>
        CrearAsync(clienteId, emailCliente, direccionEnvio, moneda, creadoEn, lineas, cancellationToken);

    public void AgregarLinea(Pedido pedido, Guid productoId, string nombreProducto, Dinero precioUnitario, int cantidad) =>
        pedido.Lineas.Add(new LineaPedido
        {
            ProductoId = productoId,
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
        Lineas: [new LineaRequest(Guid.NewGuid(), "Teclado", 150_000m, 2)]);
}
