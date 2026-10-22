using Asistente.Domain;

namespace Asistente.Application.Asistente;

public sealed record PreguntaRequest(string Pregunta, Guid? ClienteId);

public sealed record RespuestaDto(
    string Respuesta, IReadOnlyList<Fuente> Fuentes, IReadOnlyList<string> HerramientasUsadas);

/// <summary>Resultado de comprobar una conexión. <c>Estado</c>: "Ok", "Error" o "SinConfigurar".</summary>
public sealed record ComponenteSalud(string Nombre, string Conecta, string Estado, string Detalle, long DuracionMs);

/// <summary>Estado general: "Sano" si responden todas las conexiones, "Degradado" si alguna falla.</summary>
public sealed record SaludDto(string Estado, IReadOnlyList<ComponenteSalud> Componentes);

