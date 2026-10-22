using Anthropic.Exceptions;
using Asistente.Application.Asistente;
using Asistente.Application.Exceptions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Asistente.Infrastructure.IA;

/// <summary>
/// Una pregunta al modelo con sus herramientas: arma los mensajes, aplica el tiempo máximo, mide lo que consumió y
/// traduce los fallos a "servicio no disponible". Lo comparten la IA de negocio y la IA de sistemas.
/// </summary>
internal static class ConversacionIA
{
    private static readonly TimeSpan Limite = TimeSpan.FromSeconds(60);

    public static async Task<RespuestaDto> EjecutarAsync(
        IChatClient chat, string instrucciones, string pregunta, IList<AITool> herramientas, RegistroUso registro,
        int maxTokensRespuesta, string modelo, ILogger logger, CancellationToken cancellationToken)
    {
        List<ChatMessage> mensajes = [new(ChatRole.System, instrucciones), new(ChatRole.User, pregunta)];

        // Claude no admite Temperature distinta de la de fábrica: no se envía
        var opciones = new ChatOptions { MaxOutputTokens = maxTokensRespuesta, Tools = herramientas };

        using var limite = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        limite.CancelAfter(Limite);
        registro.Corte = limite;
        var reloj = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var respuesta = await chat.GetResponseAsync(mensajes, opciones, limite.Token);
            var uso = new UsoDto(modelo, respuesta.Usage?.InputTokenCount, respuesta.Usage?.OutputTokenCount, reloj.ElapsedMilliseconds);
            logger.LogInformation(
                "Pregunta respondida con {Modelo}: {TokensEntrada} tokens de entrada, {TokensSalida} de salida, {Duracion} ms, herramientas [{Herramientas}]",
                uso.Modelo, uso.TokensEntrada, uso.TokensSalida, uso.DuracionMs, string.Join(", ", registro.HerramientasUsadas()));
            return new RespuestaDto(respuesta.Text, registro.FuentesUsadas(), registro.HerramientasUsadas(), uso);
        }
        catch (Exception) when (registro.Falla is not null)
        {
            // Una herramienta no pudo consultar un servicio (p. ej. Catálogo apagado): 503 con el motivo real
            throw new ServicioNoDisponibleException(registro.Falla.Message, registro.Falla);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ServicioNoDisponibleException($"El modelo no respondió en {Limite.TotalSeconds:0} s.");
        }
        catch (AnthropicException ex)
        {
            throw new ServicioNoDisponibleException("El modelo de IA no está disponible.", ex);
        }
    }
}
