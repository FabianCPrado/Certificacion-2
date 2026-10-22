using Asistente.Application.Asistente;
using Asistente.Application.Exceptions;
using Asistente.Application.Interfaces;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Asistente.Infrastructure.IA;

/// <summary>
/// La IA que responde sobre el ecosistema (estructura, arquitectura, instrucciones) consultando la base de conocimiento
/// con sus herramientas. No toca datos de negocio ni llama a los otros sistemas.
/// </summary>
public sealed class SistemasIA(IChatClient chat, HerramientasSistemas herramientas, string modelo, ILogger<SistemasIA> logger)
    : ISistemasAsistente
{
    // Una vista general del ecosistema puede necesitar una respuesta larga
    private const int MaxTokensRespuesta = 2000;

    public Task<RespuestaDto> PreguntarAsync(string pregunta, CancellationToken cancellationToken = default)
    {
        var registro = new RegistroUso();

        return ConversacionIA.EjecutarAsync(
            chat, Instrucciones.Ecosistema, pregunta, [.. herramientas.Crear(registro)], registro,
            MaxTokensRespuesta, modelo, logger, cancellationToken);
    }
}

/// <summary>Marcador cuando no hay modelo configurado: responde 503 con el motivo en vez de fallar la inyección.</summary>
public sealed class SistemasNoConfigurado : ISistemasAsistente
{
    public Task<RespuestaDto> PreguntarAsync(string pregunta, CancellationToken cancellationToken = default) =>
        throw new ServicioNoDisponibleException(AsistenteNoConfigurado.Mensaje);
}
