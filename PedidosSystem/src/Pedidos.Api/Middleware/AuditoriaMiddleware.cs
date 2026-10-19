using System.Diagnostics;
using System.Text;
using Pedidos.Application.Interfaces.Auditoria;

namespace Pedidos.Api.Middleware;

/// <summary>
/// Audita las operaciones que modifican datos (POST, PUT y DELETE): guarda en la tabla Auditoria quién, qué ruta,
/// el cuerpo del request, el resultado y cuánto tardó. Implementa <see cref="IMiddleware"/> para que ASP.NET lo
/// resuelva del contenedor en cada request (se registra como Transient), así puede recibir el repositorio
/// (Scoped) por constructor.
/// OJO: el cuerpo se guarda tal cual llega (recortado a <see cref="MaxCuerpo"/> caracteres), por lo que puede
/// incluir datos personales como emails y direcciones.
/// </summary>
public sealed class AuditoriaMiddleware(IAuditoriaRepository auditoria, ILogger<AuditoriaMiddleware> logger) : IMiddleware
{
    private const int MaxCuerpo = 4000;

    private static readonly HashSet<string> MetodosAuditados = [HttpMethods.Post, HttpMethods.Put, HttpMethods.Delete];

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (EsOperacionAuditada(context))
        {
            await EjecutarYAuditarAsync(context, next);
        }
        else
        {
            await next(context);   // GET y demás lecturas pasan sin auditarse
        }
    }

    private static bool EsOperacionAuditada(HttpContext context) =>
        MetodosAuditados.Contains(context.Request.Method);

    private async Task EjecutarYAuditarAsync(HttpContext context, RequestDelegate next)
    {
        var inicio = Stopwatch.GetTimestamp();
        var cuerpo = await LeerCuerpoAsync(context.Request);   // antes de ejecutar: el controlador lo lee después

        try
        {
            await next(context);   // 1. se ejecuta la operación
        }
        finally
        {
            // 2. se guarda la auditoría, haya salido bien o mal. Va antes que el manejador de excepciones:
            //    aquí ya se ve el código final de la respuesta (201, 204, 400, 404, 409, 500)
            var duracionMs = (int)Stopwatch.GetElapsedTime(inicio).TotalMilliseconds;
            await RegistrarAsync(context, duracionMs, cuerpo);
        }
    }

    /// <summary>Lee el cuerpo JSON (hasta el máximo) y lo deja listo para que el controlador lo vuelva a leer.</summary>
    private static async Task<string?> LeerCuerpoAsync(HttpRequest request)
    {
        if (!request.HasJsonContentType())
            return null;

        request.EnableBuffering();   // permite volver al inicio del stream después de leerlo

        using var lector = new StreamReader(request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        var buffer = new char[MaxCuerpo];
        var leidos = await lector.ReadBlockAsync(buffer, 0, MaxCuerpo);

        request.Body.Position = 0;   // rebobinar: si no, el controlador recibiría un cuerpo vacío

        return leidos == 0 ? null : new string(buffer, 0, leidos);
    }

    private async Task RegistrarAsync(HttpContext context, int duracionMs, string? cuerpo)
    {
        var registro = new NuevaAuditoria(
            Metodo: context.Request.Method,
            Ruta: context.Request.Path.Value ?? string.Empty,
            EstadoHttp: context.Response.StatusCode,
            DuracionMs: duracionMs,
            Usuario: context.User.Identity?.Name ?? "anonimo",
            TraceId: Activity.Current?.Id ?? context.TraceIdentifier,
            Request: cuerpo);

        logger.LogInformation(
            "AUDITORIA {Metodo} {Ruta} -> {Estado} en {DuracionMs} ms · usuario {Usuario} · traza {TraceId}",
            registro.Metodo, registro.Ruta, registro.EstadoHttp, registro.DuracionMs, registro.Usuario, registro.TraceId);

        try
        {
            // CancellationToken.None: si el cliente cancela el request, la auditoría igual se guarda
            await auditoria.RegistrarAsync(registro, CancellationToken.None);
        }
        catch (Exception ex)
        {
            // Un fallo al auditar nunca debe romper la respuesta al cliente
            logger.LogError(ex, "No se pudo guardar el registro de auditoría de {Metodo} {Ruta}", registro.Metodo, registro.Ruta);
        }
    }
}
