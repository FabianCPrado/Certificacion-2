using Asistente.Application.Asistente;
using Asistente.Application.Exceptions;
using Asistente.Application.Interfaces;

namespace Asistente.Infrastructure.IA;

/// <summary>
/// Marcador hasta que se implemente el LLM (tareas 3 a 5): responde 503 en vez de fallar la inyección.
/// </summary>
public sealed class AsistenteNoConfigurado : IAsistente
{
    private const string Mensaje = "El modelo de IA todavía no está configurado.";

    public Task<RespuestaDto> ResponderAsync(PreguntaRequest pregunta, CancellationToken cancellationToken = default) =>
        throw new ServicioNoDisponibleException(Mensaje);

    public Task<PropuestaPedidoDto> ProponerPedidoAsync(PropuestaRequest solicitud, CancellationToken cancellationToken = default) =>
        throw new ServicioNoDisponibleException(Mensaje);
}
