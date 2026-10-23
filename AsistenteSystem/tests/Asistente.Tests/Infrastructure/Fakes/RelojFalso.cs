namespace Asistente.Tests.Infrastructure.Fakes;

internal sealed class RelojFalso(DateTimeOffset inicio) : TimeProvider
{
    public DateTimeOffset Ahora { get; set; } = inicio;

    public override DateTimeOffset GetUtcNow() => Ahora;
}
