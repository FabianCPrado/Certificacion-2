namespace Pedidos.Application.Pedidos.Queries.ObtenerPedido;

/// <summary>Datos de un pedido que devuelve la API. Solo transporta datos: no tiene lógica.</summary>
public sealed record PedidoDto(
    Guid Id,
    Guid ClienteId,
    string EmailCliente,
    string Estado,
    decimal Total,
    string Moneda,
    DateTime CreadoEn,
    IReadOnlyList<LineaPedidoDto> Lineas);

public sealed record LineaPedidoDto(
    string ProductoId,
    string Categoria,
    string NombreProducto,
    decimal PrecioUnitario,
    int Cantidad,
    decimal Subtotal);
