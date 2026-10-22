namespace Asistente.Domain;

/// <summary>Una herramienta que conoce el asistente (una fila de mapa-sistema.json).</summary>
public sealed record HerramientaInfo(string Nombre, string Descripcion, string Servicio, string Endpoint, bool SoloLectura);
