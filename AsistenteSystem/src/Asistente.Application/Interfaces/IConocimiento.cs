using Asistente.Domain;

namespace Asistente.Application.Interfaces;

/// <summary>La estructura de cada sistema (sistemas.json).</summary>
public interface IEstructuraSistemas
{
    IReadOnlyList<SistemaInfo> Sistemas { get; }
}

/// <summary>Los documentos de arquitectura y las instrucciones paso a paso (archivos .md).</summary>
public interface IBibliotecaDocumentos
{
    IReadOnlyList<DocumentoInfo> Documentos { get; }
}
