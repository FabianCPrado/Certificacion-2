using Anthropic;
using Anthropic.Exceptions;
using Asistente.Application.Asistente;
using Asistente.Application.Exceptions;
using Asistente.Application.Interfaces;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Asistente.Infrastructure.IA;

/// <summary>
/// Responde preguntas con un modelo de lenguaje que llama a las herramientas del mapa. La IA es un detalle de
/// Infrastructure: Application solo conoce <see cref="IAsistente"/>.
/// </summary>
public sealed class AsistenteIA(IChatClient chat, Herramientas herramientas, string modelo, ILogger<AsistenteIA> logger) : IAsistente
{
    private const int MaxTokensRespuesta = 800;
    private static readonly TimeSpan Limite = TimeSpan.FromSeconds(30);

    public async Task<RespuestaDto> ResponderAsync(PreguntaRequest pregunta, CancellationToken cancellationToken = default)
    {
        var registro = new RegistroUso();

        var texto = pregunta.ClienteId is { } cliente
            ? $"{pregunta.Pregunta}\n\n(El cliente que consulta tiene el id {cliente}.)"
            : pregunta.Pregunta;

        List<ChatMessage> mensajes = [new(ChatRole.System, Instrucciones.Sistema), new(ChatRole.User, texto)];

        // Claude no admite Temperature distinta de la de fábrica: no se envía
        var opciones = new ChatOptions { MaxOutputTokens = MaxTokensRespuesta, Tools = [.. herramientas.Crear(registro)] };

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
