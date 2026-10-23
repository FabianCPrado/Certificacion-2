using Asistente.Application.Interfaces;
using Asistente.Domain;

namespace Asistente.Tests.Application.Fakes;

internal sealed class MapaFalso(params HerramientaInfo[] herramientas) : IMapaSistema
{
    public IReadOnlyList<HerramientaInfo> Herramientas { get; } = herramientas;
}
