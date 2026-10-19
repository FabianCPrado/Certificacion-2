using MediatR;
using Pedidos.Application.Common;

namespace Pedidos.Application.Pedidos.Commands.CrearPedido;

/// <summary>Datos para crear un pedido. Devuelve el Id del pedido creado.</summary>
public sealed record CrearPedidoCommand(
    Guid ClienteId,
    string EmailCliente,
    DireccionRequest Direccion,
    string Moneda,
    IReadOnlyList<LineaRequest> Lineas) : ICommand<Guid>;

public sealed record DireccionRequest(string Calle, string Ciudad, string CodigoPostal, string Pais);

public sealed record LineaRequest(Guid ProductoId, string NombreProducto, decimal PrecioUnitario, int Cantidad);
