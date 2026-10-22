using Asistente.Application.Asistente;

namespace Asistente.Application.Interfaces;

/// <summary>
/// Comprueba si el asistente está conectado a lo que necesita. Solo se conecta cuando se le pide:
/// el asistente no abre ninguna conexión al arrancar.
/// </summary>
public interface IVerificadorSalud
{
    Task<SaludDto> VerificarAsync(CancellationToken cancellationToken = default);
}
