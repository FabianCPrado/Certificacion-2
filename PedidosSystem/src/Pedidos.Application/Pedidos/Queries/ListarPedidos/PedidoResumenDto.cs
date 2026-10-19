namespace Pedidos.Application.Pedidos.Queries.ListarPedidos;

/// <summary>Resumen de un pedido para listados: sin líneas, solo las columnas necesarias.</summary>
public sealed record PedidoResumenDto(
    Guid Id,
    Guid ClienteId,
    string Estado,
    decimal Total,
    string Moneda,
    DateTime CreadoEn);

public sealed record LineaResumenDto(string ProductoId, int Cantidad);

public sealed record PedidoConLineasDto(Guid Id, List<LineaResumenDto> Lineas);
