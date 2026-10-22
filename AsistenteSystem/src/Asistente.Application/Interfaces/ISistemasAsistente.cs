using Asistente.Application.Asistente;

namespace Asistente.Application.Interfaces;

/// <summary>
/// Puerto de la IA que responde sobre el ecosistema (estructura, arquitectura, instrucciones). Es distinto de
/// <see cref="IAsistente"/>, que responde sobre datos de negocio. La implementación vive en Infrastructure.
/// </summary>
public interface ISistemasAsistente
{
    Task<RespuestaDto> PreguntarAsync(string pregunta, CancellationToken cancellationToken = default);
}
