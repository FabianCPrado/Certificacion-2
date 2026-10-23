using Asistente.Application.Interfaces;
using Asistente.Domain;

namespace Asistente.Tests.Application.Fakes;

internal sealed class BibliotecaFalsa(params DocumentoInfo[] documentos) : IBibliotecaDocumentos
{
    public IReadOnlyList<DocumentoInfo> Documentos { get; } = documentos;
}
