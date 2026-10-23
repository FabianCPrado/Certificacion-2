using Asistente.Infrastructure.Mapa;
using Asistente.Tests.Contratos;

namespace Asistente.Tests.Infrastructure.Mapa;

public class BibliotecaDocumentosTests
{
    [Fact]
    public void Los_documentos_reales_se_leen_con_titulo_y_resumen()
    {
        if (RepositorioReal.Raiz() is null) return;

        var biblioteca = new BibliotecaDocumentos(RepositorioReal.DeAsistente("docs"));

        Assert.Equal(7, biblioteca.Documentos.Count(d => d.Tipo == "arquitectura"));
        Assert.Equal(5, biblioteca.Documentos.Count(d => d.Tipo == "instrucciones"));
        Assert.All(biblioteca.Documentos, d =>
        {
            Assert.NotEmpty(d.Titulo);
            Assert.NotEmpty(d.Resumen);
            Assert.DoesNotMatch(@"^\d", d.Id);
        });
    }
}
