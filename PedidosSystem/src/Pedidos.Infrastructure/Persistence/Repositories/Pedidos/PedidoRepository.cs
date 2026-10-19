using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Pedidos.Domain.Common;
using Pedidos.Application.Interfaces.Pedidos;
using Pedidos.Domain.Pedidos;
using Pedidos.Domain.ValueObjects;

namespace Pedidos.Infrastructure.Persistence.Repositories.Pedidos;

public sealed class PedidoRepository(AppDbContext context) : IPedidoRepository
{
    public Task<Pedido?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Pedidos
            .Include(p => p.Lineas)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Pedido Crear(Guid clienteId, Email emailCliente, Direccion direccionEnvio, string moneda, DateTime creadoEn)
    {
        if (clienteId == Guid.Empty)
            throw new ReglaNegocioException("El pedido debe tener un cliente.");

        if (!Dinero.EsMonedaValida(moneda))
            throw new ReglaNegocioException("La moneda debe ser un código ISO de 3 letras en mayúsculas.");

        var pedido = new Pedido
        {
            ClienteId = clienteId,
            EmailCliente = emailCliente,
            DireccionEnvio = direccionEnvio,
            Moneda = moneda,
            CreadoEn = creadoEn,
            Estado = EstadoPedido.Borrador
        };

        context.Pedidos.Add(pedido);
        return pedido;
    }

    public async Task<Pedido> CrearAsync(
        Guid clienteId, Email emailCliente, Direccion direccionEnvio, string moneda, DateTime creadoEn,
        IReadOnlyList<LineaNueva> lineas, CancellationToken cancellationToken = default)
    {
        var pedido = Crear(clienteId, emailCliente, direccionEnvio, moneda, creadoEn);

        foreach (var linea in lineas)
            AgregarLinea(pedido, linea.ProductoId, linea.NombreProducto, linea.PrecioUnitario, linea.Cantidad);

        await context.SaveChangesAsync(cancellationToken);
        return pedido;
    }

    public async Task<Pedido> CrearYDescontarStockAsync(
        Guid clienteId, Email emailCliente, Direccion direccionEnvio, string moneda, DateTime creadoEn,
        IReadOnlyList<LineaNueva> lineas, CancellationToken cancellationToken = default)
    {
        // Transacción explícita: los SaveChanges y el procedimiento almacenado de abajo comparten la misma
        // conexión y transacción. Se confirman juntos (Commit) o no queda nada (Rollback).
        await using var transaccion = await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var pedido = await CrearAsync(clienteId, emailCliente, direccionEnvio, moneda, creadoEn, lineas, cancellationToken);

            foreach (var linea in lineas)
                await DescontarStockAsync(linea.ProductoId, linea.Cantidad, cancellationToken);

            await transaccion.CommitAsync(cancellationToken);
            return pedido;
        }
        catch
        {
            await transaccion.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task DescontarStockAsync(Guid productoId, int cantidad, CancellationToken cancellationToken)
    {
        var stock = await context.Productos
            .Where(p => p.Id == productoId)
            .Select(p => (int?)p.Stock)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ReglaNegocioException($"No existe el producto {productoId}.");

        if (stock < cantidad)
            throw new ReglaNegocioException($"No hay stock suficiente del producto {productoId}.");

        await context.Database.ExecuteSqlAsync($"EXEC ActualizarStock {productoId}, {cantidad}", cancellationToken);
    }

    public void AgregarLinea(Pedido pedido, Guid productoId, string nombreProducto, Dinero precioUnitario, int cantidad)
    {
        if (pedido.Estado != EstadoPedido.Borrador)
            throw new ReglaNegocioException("Solo se puede modificar un pedido en borrador.");

        if (cantidad <= 0)
            throw new ReglaNegocioException("La cantidad debe ser mayor que cero.");

        if (precioUnitario.Moneda != pedido.Moneda)
            throw new ReglaNegocioException("El precio debe estar en la moneda del pedido.");

        var existente = pedido.Lineas.Find(l => l.ProductoId == productoId);
        if (existente is null)
        {
            pedido.Lineas.Add(new LineaPedido
            {
                ProductoId = productoId,
                NombreProducto = nombreProducto,
                PrecioUnitario = precioUnitario,
                Cantidad = cantidad
            });
        }
        else
        {
            existente.Cantidad += cantidad;
        }
    }

    public void Confirmar(Pedido pedido)
    {
        if (pedido.Estado != EstadoPedido.Borrador)
            throw new ReglaNegocioException("Solo se puede confirmar un pedido en borrador.");

        if (pedido.Lineas.Count == 0)
            throw new ReglaNegocioException("No se puede confirmar un pedido sin líneas.");

        pedido.Estado = EstadoPedido.Confirmado;
    }

    public void Cancelar(Pedido pedido)
    {
        if (pedido.Estado == EstadoPedido.Cancelado)
            throw new ReglaNegocioException("El pedido ya está cancelado.");

        pedido.Estado = EstadoPedido.Cancelado;
    }

    public async Task ConfirmarAsync(Pedido pedido, CancellationToken cancellationToken = default)
    {
        Confirmar(pedido);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task CancelarAsync(Pedido pedido, CancellationToken cancellationToken = default)
    {
        Cancelar(pedido);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task ActualizarAsync(
        Pedido pedido, Email emailCliente, Direccion direccionEnvio, CancellationToken cancellationToken = default)
    {
        if (pedido.Estado != EstadoPedido.Borrador)
            throw new ReglaNegocioException("Solo se puede modificar un pedido en borrador.");

        pedido.EmailCliente = emailCliente;
        pedido.DireccionEnvio = direccionEnvio;

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task EliminarAsync(Pedido pedido, CancellationToken cancellationToken = default)
    {
        if (pedido.Estado != EstadoPedido.Borrador)
            throw new ReglaNegocioException("Solo se puede eliminar un pedido en borrador.");

        context.Pedidos.Remove(pedido);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> CancelarBorradoresAntiguosAsync(int dias, CancellationToken cancellationToken = default)
    {
        if (dias <= 0)
            throw new ReglaNegocioException("Los días deben ser mayor que cero.");

        // Dapper ejecuta el procedimiento almacenado CancelarBorradoresAntiguos (scripts/procedimientos-clase4.sql).
        // El resultado vuelve en un parámetro de salida: no hay filas que leer, solo cuántos pedidos se cancelaron.
        await using var conexion = new SqlConnection(context.Database.GetConnectionString());

        var parametros = new DynamicParameters();
        parametros.Add("Dias", dias);
        parametros.Add("Cancelados", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await conexion.ExecuteAsync(new CommandDefinition(
            "CancelarBorradoresAntiguos",
            parametros,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken));

        return parametros.Get<int>("Cancelados");
    }}
