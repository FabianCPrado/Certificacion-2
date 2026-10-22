namespace Asistente.Domain;

/// <summary>De donde salio un dato de la respuesta, p. ej. ("Catalogo", "Abarrotes/yerba-1kg").</summary>
public sealed record Fuente(string Servicio, string Referencia);
