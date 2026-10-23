using Asistente.Infrastructure.Mapa;
using Asistente.Tests.Contratos;

namespace Asistente.Tests.Infrastructure.Mapa;

public class EstructuraSistemasTests
{
    [Fact]
    public void El_sistemas_json_real_define_los_cuatro_sistemas_con_su_contenido()
    {
        if (RepositorioReal.Raiz() is null) return;

        var sistemas = new EstructuraSistemas(RepositorioReal.DeAsistente("src/Asistente.Api/sistemas.json")).Sistemas;

        Assert.Equal(["Authentication", "Catalogo", "Pedidos", "Clientes"], sistemas.Select(s => s.Nombre));
        Assert.All(sistemas, s =>
        {
            Assert.NotEmpty(s.Endpoints);
            Assert.NotEmpty(s.Capas);
            Assert.NotEmpty(s.Tests);
        });
    }

    [Fact]
    public void Todo_lo_que_un_sistema_consume_existe_como_sistema()
    {
        if (RepositorioReal.Raiz() is null) return;

        var sistemas = new EstructuraSistemas(RepositorioReal.DeAsistente("src/Asistente.Api/sistemas.json")).Sistemas;
        var nombres = sistemas.Select(s => s.Nombre).ToHashSet();

        foreach (var dependencia in sistemas.SelectMany(s => s.Consume))
            Assert.Contains(dependencia.Sistema, nombres);
    }
}
