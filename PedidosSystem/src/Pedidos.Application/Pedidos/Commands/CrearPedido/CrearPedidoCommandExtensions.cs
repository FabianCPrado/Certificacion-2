using Pedidos.Application.Exceptions;
using Pedidos.Application.Interfaces.Pedidos;
using Pedidos.Application.Interfaces.Productos;
using Pedidos.Domain.ValueObjects;

namespace Pedidos.Application.Pedidos.Commands.CrearPedido;

/// <summary>Convierte los datos de entrada del comando en objetos de valor y líneas del dominio.</summary>
public static class CrearPedidoCommandExtensions
{
    public static Email ToEmail(this CrearPedidoCommand comando) => new(comando.EmailCliente);

    public static Direccion ToDireccion(this CrearPedidoCommand comando) => new(
        comando.Direccion.Calle, comando.Direccion.Ciudad, comando.Direccion.CodigoPostal, comando.Direccion.Pais);

    /// <summary>
    /// Arma las líneas con el nombre y el precio que tiene hoy el servicio Catálogo (el cliente no los envía).
    /// Lanza <see cref="NotFoundException"/> si algún producto no existe.
    /// </summary>
    public static async Task<IReadOnlyList<LineaNueva>> ResolverLineasAsync(
        this ICatalogoProductos catalogo, CrearPedidoCommand comando, CancellationToken cancellationToken)
    {
        var lineas = new List<LineaNueva>(comando.Lineas.Count);

        foreach (var linea in comando.Lineas)
        {
            var producto = await catalogo.ObtenerAsync(linea.Categoria, linea.ProductoId, cancellationToken)
                ?? throw new NotFoundException($"No existe el producto {linea.ProductoId} en la categoría {linea.Categoria}.");

            lineas.Add(new LineaNueva(
                producto.Id, producto.Categoria, producto.Nombre, new Dinero(producto.Precio, comando.Moneda), linea.Cantidad));
        }

        return lineas;
    }
}
