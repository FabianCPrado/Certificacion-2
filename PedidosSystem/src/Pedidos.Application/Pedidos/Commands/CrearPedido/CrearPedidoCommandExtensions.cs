using Pedidos.Application.Interfaces.Pedidos;
using Pedidos.Domain.ValueObjects;

namespace Pedidos.Application.Pedidos.Commands.CrearPedido;

/// <summary>Convierte los datos de entrada del comando en objetos de valor y líneas del dominio.</summary>
public static class CrearPedidoCommandExtensions
{
    public static Email ToEmail(this CrearPedidoCommand comando) => new(comando.EmailCliente);

    public static Direccion ToDireccion(this CrearPedidoCommand comando) => new(
        comando.Direccion.Calle, comando.Direccion.Ciudad, comando.Direccion.CodigoPostal, comando.Direccion.Pais);

    public static IReadOnlyList<LineaNueva> ToLineasNuevas(this CrearPedidoCommand comando) =>
        comando.Lineas
            .Select(l => new LineaNueva(l.ProductoId, l.NombreProducto, new Dinero(l.PrecioUnitario, comando.Moneda), l.Cantidad))
            .ToList();
}
