using Asistente.Infrastructure.Mapa;

namespace Asistente.Tests.Infrastructure.Mapa;

public class MapaSistemaTests
{
    [Fact]
    public void El_mapa_del_api_define_cinco_herramientas_de_solo_lectura()
    {
        var ruta = Path.Combine(AppContext.BaseDirectory, "mapa-sistema.json");
        var mapa = new MapaSistema(ruta);

        Assert.Equal(5, mapa.Herramientas.Count);
        Assert.All(mapa.Herramientas, h => Assert.True(h.SoloLectura));
    }
}
