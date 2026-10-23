using Asistente.Application.Interfaces;
using Asistente.Domain;

namespace Asistente.Tests.Application.Fakes;

internal sealed class EstructuraFalsa(params SistemaInfo[] sistemas) : IEstructuraSistemas
{
    public IReadOnlyList<SistemaInfo> Sistemas { get; } = sistemas;
}
