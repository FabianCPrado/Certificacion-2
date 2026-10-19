namespace Pedidos.Application.Exceptions;

/// <summary>Se lanza cuando no existe el recurso pedido. La API la devuelve como 404.</summary>
public sealed class NotFoundException(string mensaje) : Exception(mensaje);
