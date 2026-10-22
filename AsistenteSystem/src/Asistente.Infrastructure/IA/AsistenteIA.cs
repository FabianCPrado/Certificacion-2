using Anthropic;
using Anthropic.Exceptions;
using Asistente.Application.Asistente;
using Asistente.Application.Exceptions;
using Asistente.Application.Interfaces;
using Microsoft.Extensions.AI;

namespace Asistente.Infrastructure.IA;

/// <summary>
/// Responde preguntas con un modelo de lenguaje que llama a las herramientas del mapa. La IA es un detalle de
/// Infrastructure: Application solo conoce <see cref="IAsistente"/>. Proponer pedidos llega con el agente (tarea 5).
/// </summary>
public sealed class AsistenteIA(IChatClient chat, Herramientas herramientas) : IAsistente
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

        try
        {
            var respuesta = await chat.GetResponseAsync(mensajes, opciones, limite.Token);
            return new RespuestaDto(respuesta.Text, registro.FuentesUsadas(), registro.HerramientasUsadas());
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

    public Task<PropuestaPedidoDto> ProponerPedidoAsync(PropuestaRequest solicitud, CancellationToken cancellationToken = default) =>
        throw new ServicioNoDisponibleException("Proponer pedidos todavía no está disponible (tarea 5).");
}
