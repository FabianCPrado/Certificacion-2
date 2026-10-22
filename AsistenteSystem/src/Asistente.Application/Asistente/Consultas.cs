namespace Asistente.Application.Asistente;

// Lo que el asistente conoce de los otros servicios. Son tipos propios y mínimos:
// no tienen email ni datos sensibles, así que nunca llegan al modelo.

public sealed record ProductoInfo(string Categoria, string Id, string Nombre, decimal Precio, int Stock);

public sealed record LineaInfo(string ProductoId, string Categoria, string NombreProducto, decimal PrecioUnitario, int Cantidad);

public sealed record PedidoInfo(
    Guid Id, Guid ClienteId, string Estado, decimal Total, string Moneda, DateTime CreadoEn, IReadOnlyList<LineaInfo> Lineas);

public sealed record ClienteInfo(Guid Id, string Nombre);
