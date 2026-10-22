using Asistente.Application.Asistente;
using Asistente.Application.Exceptions;
using Asistente.Application.Interfaces;

namespace Asistente.Infrastructure.IA;

/// <summary>
/// Marcador cuando no hay modelo configurado: responde 503 en vez de fallar la inyección.
/// </summary>
public sealed class AsistenteNoConfigurado : IAsistente
{
    private const string Mensaje = "El modelo de IA no está configurado: falta IA:Proveedor = Anthropic o la clave IA:ApiKey (User Secrets).";

    public Task<RespuestaDto> ResponderAsync(PreguntaRequest pregunta, CancellationToken cancellationToken = default) =>
        throw new ServicioNoDisponibleException(Mensaje);
}
