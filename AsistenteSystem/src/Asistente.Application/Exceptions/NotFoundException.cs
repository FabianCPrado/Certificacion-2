namespace Asistente.Application.Exceptions;

/// <summary>No existe lo que se pidio (p. ej. un cliente sin pedidos). La API lo traduce a 404.</summary>
public sealed class NotFoundException(string message) : Exception(message);
