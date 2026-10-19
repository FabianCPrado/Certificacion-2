using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Pedidos.Application.Common;
using Pedidos.Application.Interfaces.Pedidos;
using Pedidos.Application.Interfaces.Productos;
using Pedidos.Application.Pedidos.Commands.CrearPedido;

namespace Pedidos.Application.Pedidos.Commands.CrearPedidoConStock;

/// <summary>
/// Crea el pedido con sus líneas y descuenta el stock de cada producto en el servicio Catálogo.
/// Como son dos bases distintas no hay una transacción única: si falla un paso (producto inexistente, stock insuficiente,
/// error al guardar el pedido) se devuelve el stock ya descontado. Devuelve el Id del pedido.
/// </summary>
public sealed record CrearPedidoConStockCommand(CrearPedidoCommand Pedido) : ICommand<Guid>;

public sealed class CrearPedidoConStockCommandValidator : AbstractValidator<CrearPedidoConStockCommand>
{
    public CrearPedidoConStockCommandValidator() =>
        RuleFor(c => c.Pedido).NotNull().SetValidator(new CrearPedidoCommandValidator());
}

public sealed class CrearPedidoConStockCommandHandler(
    IPedidoRepository pedidos, ICatalogoProductos catalogo, TimeProvider tiempo, ILogger<CrearPedidoConStockCommandHandler> log)
    : IRequestHandler<CrearPedidoConStockCommand, Guid>
{
    public async Task<Guid> Handle(CrearPedidoConStockCommand request, CancellationToken cancellationToken)
    {
        var comando = request.Pedido;
        var lineas = await catalogo.ResolverLineasAsync(comando, cancellationToken);
        var descontadas = new List<LineaNueva>();

        try
        {
            foreach (var linea in lineas)
            {
                await catalogo.DescontarStockAsync(linea.Categoria, linea.ProductoId, linea.Cantidad, cancellationToken);
                descontadas.Add(linea);
            }

            var pedido = await pedidos.CrearAsync(
                comando.ClienteId, comando.ToEmail(), comando.ToDireccion(), comando.Moneda,
                tiempo.GetUtcNow().UtcDateTime, lineas, cancellationToken);

            return pedido.Id;
        }
        catch
        {
            await DevolverStockAsync(descontadas);
            throw;
        }
    }

    // Compensación: si devolver una línea falla, se sigue con las demás y queda en el log para revisarlo a mano.
    private async Task DevolverStockAsync(List<LineaNueva> descontadas)
    {
        foreach (var linea in descontadas)
        {
            try
            {
                await catalogo.ReponerStockAsync(linea.Categoria, linea.ProductoId, linea.Cantidad, CancellationToken.None);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "No se pudo devolver el stock de {Producto} ({Cantidad})", linea.ProductoId, linea.Cantidad);
            }
        }
    }
}
