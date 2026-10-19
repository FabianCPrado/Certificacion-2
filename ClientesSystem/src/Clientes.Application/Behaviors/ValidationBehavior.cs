using FluentValidation;
using MediatR;

namespace Clientes.Application.Behaviors;

/// <summary>
/// Ejecuta los validadores de FluentValidation antes del handler.
/// Si hay errores lanza <see cref="ValidationException"/>, que la API traduce a 400.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next(cancellationToken);

        var contexto = new ValidationContext<TRequest>(request);
        var resultados = await Task.WhenAll(validators.Select(v => v.ValidateAsync(contexto, cancellationToken)));

        var errores = resultados.SelectMany(r => r.Errors).ToList();
        if (errores.Count != 0)
            throw new ValidationException(errores);

        return await next(cancellationToken);
    }
}
