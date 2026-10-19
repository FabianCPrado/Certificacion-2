using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Catalogo.Application.Exceptions;
using Catalogo.Domain.Common;

namespace Catalogo.Api.Infrastructure;

/// <summary>
/// Único lugar donde las excepciones se convierten en respuestas HTTP (ProblemDetails):
/// ValidationException → 400 · NotFoundException → 404 · ReglaNegocioException → 409 · otra → 500.
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problem = exception switch
        {
            ValidationException validacion => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Error de validación",
                Extensions =
                {
                    ["errores"] = validacion.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
                }
            },
            NotFoundException => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "No encontrado",
                Detail = exception.Message
            },
            ReglaNegocioException => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Regla de negocio",
                Detail = exception.Message
            },
            // SQL Server no respondió
            Microsoft.Data.SqlClient.SqlException => new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "Servicio no disponible",
                Detail = "No se pudo comunicar con la base de datos del catálogo."
            },
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Error interno",
                Detail = "Ocurrió un error inesperado."
            }
        };

        if (problem.Status == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Excepción no controlada");

        httpContext.Response.StatusCode = problem.Status!.Value;

        problem.Extensions["traceId"] = System.Diagnostics.Activity.Current?.Id ?? httpContext.TraceIdentifier;

        // Se escribe directo: IProblemDetailsService puede devolver false según el header Accept
        // y entonces ASP.NET responde un 400 genérico sin nuestros errores.
        await httpContext.Response.WriteAsJsonAsync(
            problem, options: null, contentType: "application/problem+json", cancellationToken);

        return true;
    }
}



