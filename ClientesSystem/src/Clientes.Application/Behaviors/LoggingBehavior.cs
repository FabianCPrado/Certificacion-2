using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Clientes.Application.Behaviors;

/// <summary>Registra cada request que pasa por MediatR y cuánto tardó.</summary>
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var nombre = typeof(TRequest).Name;
        var inicio = Stopwatch.GetTimestamp();

        var respuesta = await next(cancellationToken);

        logger.LogInformation("{Request} completado en {Milisegundos:0} ms", nombre, Stopwatch.GetElapsedTime(inicio).TotalMilliseconds);
        return respuesta;
    }
}
