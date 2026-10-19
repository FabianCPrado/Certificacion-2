using System.Diagnostics;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace Catalogo.Api.Grpc;

/// <summary>Deja una línea de log por cada llamada gRPC con el método y el tiempo que tardó.</summary>
public class LoggingInterceptor(ILogger<LoggingInterceptor> log) : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            return await continuation(request, context);
        }
        finally
        {
            log.LogInformation("gRPC {Metodo} {Ms} ms", context.Method, sw.ElapsedMilliseconds);
        }
    }
}
