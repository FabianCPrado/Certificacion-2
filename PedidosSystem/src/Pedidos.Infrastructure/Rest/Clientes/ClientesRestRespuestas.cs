using System.Net;
using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using Pedidos.Application.Exceptions;
using Pedidos.Domain.Common;

namespace Pedidos.Infrastructure.Rest.Clientes;

/// <summary>
/// Traduce las respuestas del servicio Clientes a las mismas excepciones que antes lanzaba la base local,
/// para que Application y la API no noten el cambio: 400 → ValidationException · 404 → NotFoundException ·
/// 409 → ReglaNegocioException · otro error → HttpRequestException (la API responde 503).
/// </summary>
internal static class ClientesRestRespuestas
{
    public static async Task AsegurarExitoAsync(HttpResponseMessage respuesta, CancellationToken cancellationToken)
    {
        if (respuesta.IsSuccessStatusCode)
            return;

        var cuerpo = await respuesta.Content.ReadAsStringAsync(cancellationToken);
        var detalle = LeerDetalle(cuerpo) ?? respuesta.ReasonPhrase ?? "Error en el servicio Clientes.";

        throw respuesta.StatusCode switch
        {
            HttpStatusCode.BadRequest => new ValidationException(LeerErrores(cuerpo, detalle)),
            HttpStatusCode.NotFound => new NotFoundException(detalle),
            HttpStatusCode.Conflict => new ReglaNegocioException(detalle),
            _ => new HttpRequestException($"El servicio Clientes respondió {(int)respuesta.StatusCode}: {detalle}")
        };
    }

    private static string? LeerDetalle(string cuerpo)
    {
        try
        {
            using var json = JsonDocument.Parse(cuerpo);
            return json.RootElement.TryGetProperty("detail", out var detalle) ? detalle.GetString() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    // El servicio Clientes envía los errores de validación en "errores": { campo: [mensajes] }
    private static List<ValidationFailure> LeerErrores(string cuerpo, string detalle)
    {
        var fallos = new List<ValidationFailure>();
        try
        {
            using var json = JsonDocument.Parse(cuerpo);
            if (json.RootElement.TryGetProperty("errores", out var errores) && errores.ValueKind == JsonValueKind.Object)
            {
                foreach (var campo in errores.EnumerateObject())
                    foreach (var mensaje in campo.Value.EnumerateArray())
                        fallos.Add(new ValidationFailure(campo.Name, mensaje.GetString()));
            }
        }
        catch (JsonException)
        {
            // Si el cuerpo no es JSON se usa el detalle genérico
        }

        if (fallos.Count == 0)
            fallos.Add(new ValidationFailure(string.Empty, detalle));

        return fallos;
    }
}
