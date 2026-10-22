using Asistente.Domain;

namespace Asistente.Application.Asistente;

public sealed record PreguntaRequest(string Pregunta, Guid? ClienteId);

public sealed record RespuestaDto(
    string Respuesta, IReadOnlyList<Fuente> Fuentes, IReadOnlyList<string> HerramientasUsadas);

public sealed record PropuestaRequest(Guid ClienteId, string Pedido);

public sealed record LineaPropuesta(
    string Categoria, string ProductoId, string Nombre, int Cantidad, decimal PrecioUnitario, int StockDisponible);

/// <summary>Propuesta de pedido: el asistente nunca lo crea, lo confirma una persona en Pedidos.</summary>
public sealed record PropuestaPedidoDto(
    Guid ClienteId, IReadOnlyList<LineaPropuesta> Lineas, decimal TotalEstimado,
    string Explicacion, bool RequiereConfirmacion = true);
