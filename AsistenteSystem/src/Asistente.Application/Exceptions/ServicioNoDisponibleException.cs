namespace Asistente.Application.Exceptions;

/// <summary>El modelo de IA o un servicio del que depende el asistente no responde. La API lo traduce a 503.</summary>
public sealed class ServicioNoDisponibleException(string message, Exception? inner = null) : Exception(message, inner);
