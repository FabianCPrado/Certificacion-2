using Asistente.Domain;

namespace Asistente.Application.Interfaces;

/// <summary>Lista de herramientas que conoce el asistente (mapa-sistema.json).</summary>
public interface IMapaSistema
{
    IReadOnlyList<HerramientaInfo> Herramientas { get; }
}
