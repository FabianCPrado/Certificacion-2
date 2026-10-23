using Microsoft.Extensions.AI;

namespace Asistente.Tests.Infrastructure.Fakes;

/// <summary>Un modelo de mentira: devuelve las respuestas que se le cargan, en orden.</summary>
internal sealed class ModeloFalso(params ChatResponse[] respuestas) : IChatClient
{
    private readonly Queue<ChatResponse> _respuestas = new(respuestas);

    public List<IList<ChatMessage>> Recibido { get; } = [];

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        Recibido.Add([.. messages]);
        return Task.FromResult(_respuestas.Dequeue());
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}
