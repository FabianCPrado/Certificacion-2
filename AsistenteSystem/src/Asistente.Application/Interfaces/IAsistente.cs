using Asistente.Application.Asistente;

namespace Asistente.Application.Interfaces;

/// <summary>Puerto principal. La implementacion (LLM, RAG) vive en Infrastructure.</summary>
public interface IAsistente
{
    Task<RespuestaDto> ResponderAsync(PreguntaRequest pregunta, CancellationToken cancellationToken = default);
}
