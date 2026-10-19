namespace Pedidos.Domain.Common;

/// <summary>Se lanza cuando una operación viola una regla del negocio. La API la devuelve como 409.</summary>
public sealed class ReglaNegocioException(string mensaje) : Exception(mensaje);
