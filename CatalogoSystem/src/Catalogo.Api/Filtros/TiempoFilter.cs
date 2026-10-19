using System.Diagnostics;

namespace Catalogo.Api.Filtros;

/// <summary>Filtro de endpoint: mide cuánto tarda el endpoint, lo deja en el header X-Tiempo-ms y lo registra en el log.</summary>
public class TiempoFilter(ILogger<TiempoFilter> log) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var sw = Stopwatch.StartNew();
        var resultado = await next(context);   // el endpoint
        context.HttpContext.Response.Headers["X-Tiempo-ms"] = sw.ElapsedMilliseconds.ToString();
        log.LogInformation("{Ruta} {Ms} ms", context.HttpContext.Request.Path, sw.ElapsedMilliseconds);
        return resultado;
    }
}

