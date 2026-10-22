using Asistente.Domain;

namespace Asistente.Application.Asistente;

public sealed record PreguntaRequest(string Pregunta, Guid? ClienteId);

/// <summary>Lo que consumió la pregunta en el modelo: sirve para compararlo con el consumo de la Consola de Anthropic.</summary>
public sealed record UsoDto(string Modelo, long? TokensEntrada, long? TokensSalida, long DuracionMs);

public sealed record RespuestaDto(
    string Respuesta, IReadOnlyList<Fuente> Fuentes, IReadOnlyList<string> HerramientasUsadas, UsoDto? Uso = null);

/// <summary>Resultado de comprobar una conexión. <c>Estado</c>: "Ok", "Error" o "SinConfigurar".</summary>
public sealed record ComponenteSalud(string Nombre, string Conecta, string Estado, string Detalle, long DuracionMs);

/// <summary>Estado general: "Sano" si responden todas las conexiones, "Degradado" si alguna falla.</summary>
public sealed record SaludDto(string Estado, IReadOnlyList<ComponenteSalud> Componentes);

