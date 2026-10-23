using System.Net;
using System.Text;

namespace Asistente.Tests.Infrastructure.Fakes;

/// <summary>Un servidor de mentira: responde lo que se le indica y guarda lo que recibió.</summary>
internal sealed class ServidorFalso(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
{
    public List<HttpRequestMessage> Pedidos { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Pedidos.Add(request);
        return Task.FromResult(responder(request));
    }

    public static HttpResponseMessage Json(string json, HttpStatusCode estado = HttpStatusCode.OK) =>
        new(estado) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
}
