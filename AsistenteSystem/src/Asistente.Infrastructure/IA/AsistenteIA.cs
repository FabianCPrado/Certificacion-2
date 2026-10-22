using Asistente.Application.Asistente;
using Asistente.Application.Interfaces;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Asistente.Infrastructure.IA;

/// <summary>
/// Responde preguntas de negocio con un modelo de lenguaje que llama a las herramientas del mapa. La IA es un detalle
/// de Infrastructure: Application solo conoce <see cref="IAsistente"/>.
/// </summary>
public sealed class AsistenteIA(IChatClient chat, Herramientas herramientas, string modelo, ILogger<AsistenteIA> logger) : IAsistente
{
    private const int MaxTokensRespuesta = 800;

    public Task<RespuestaDto> ResponderAsync(PreguntaRequest pregunta, CancellationToken cancellationToken = default)
    {
        var registro = new RegistroUso();

        var texto = pregunta.ClienteId is { } cliente
            ? $"{pregunta.Pregunta}\n\n(El cliente que consulta tiene el id {cliente}.)"
            : pregunta.Pregunta;

        return ConversacionIA.EjecutarAsync(
            chat, Instrucciones.Sistema, texto, [.. herramientas.Crear(registro)], registro,
            MaxTokensRespuesta, modelo, logger, cancellationToken);
    }
}
