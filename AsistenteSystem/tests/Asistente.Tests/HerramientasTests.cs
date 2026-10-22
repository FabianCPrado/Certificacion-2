using Asistente.Application.Asistente;
using Asistente.Application.Interfaces;
using Asistente.Domain;
using Asistente.Infrastructure.IA;
using NSubstitute;

namespace Asistente.Tests;

internal sealed class MapaFalso(params HerramientaInfo[] herramientas) : IMapaSistema
{
    public IReadOnlyList<HerramientaInfo> Herramientas { get; } = herramientas;
}

public class HerramientasTests
{
    private static readonly string[] Nombres =
    [
        Infrastructure.IA.Herramientas.ObtenerPrecioYStock, Infrastructure.IA.Herramientas.BuscarProductos,
        Infrastructure.IA.Herramientas.UltimosPedidosDeCliente, Infrastructure.IA.Herramientas.EstadoDePedido,
        Infrastructure.IA.Herramientas.DatosDeCliente
    ];

    private static HerramientaInfo Info(string nombre, bool soloLectura = true) =>
        new(nombre, $"Descripción de {nombre}", "Servicio", "GET /x", soloLectura);

    private static Infrastructure.IA.Herramientas Crear(params HerramientaInfo[] mapa) =>
        new(new MapaFalso(mapa), Substitute.For<ICatalogoConsultas>(),
            Substitute.For<IPedidosConsultas>(), Substitute.For<IClientesConsultas>());

    [Fact]
    public void Validar_acepta_un_mapa_que_coincide_con_el_codigo() =>
        Assert.Null(Record.Exception(() => Infrastructure.IA.Herramientas.Validar([.. Nombres.Select(n => Info(n))])));

    [Fact]
    public void Validar_falla_si_una_herramienta_del_codigo_no_esta_en_el_mapa()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => Infrastructure.IA.Herramientas.Validar([.. Nombres.Skip(1).Select(n => Info(n))]));

        Assert.Contains(Nombres[0], ex.Message);
    }

    [Fact]
    public void Validar_falla_si_el_mapa_tiene_una_herramienta_que_el_codigo_no_implementa()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => Infrastructure.IA.Herramientas.Validar([.. Nombres.Select(n => Info(n)), Info("CrearPedido")]));

        Assert.Contains("CrearPedido", ex.Message);
    }

    [Fact]
    public void Solo_se_registran_las_herramientas_de_solo_lectura()
    {
        var mapa = Nombres.Select((n, i) => Info(n, soloLectura: i != 0)).ToArray();

        var herramientas = Crear(mapa).Crear(new RegistroUso());

        Assert.Equal(Nombres.Length - 1, herramientas.Count);
        Assert.DoesNotContain(herramientas, h => h.Name == Nombres[0]);
    }

    [Fact]
    public void Nombre_y_descripcion_de_cada_herramienta_salen_del_mapa()
    {
        var herramientas = Crear([.. Nombres.Select(n => Info(n))]).Crear(new RegistroUso());

        Assert.All(herramientas, h => Assert.Equal($"Descripción de {h.Name}", h.Description));
    }

    [Fact]
    public void El_mapa_real_coincide_con_el_codigo()
    {
        var ruta = Path.Combine(AppContext.BaseDirectory, "mapa-sistema.json");
        if (!File.Exists(ruta))
            ruta = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/Asistente.Api/mapa-sistema.json"));

        var mapa = new Infrastructure.Mapa.MapaSistema(ruta);

        Infrastructure.IA.Herramientas.Validar(mapa.Herramientas);
        Assert.All(mapa.Herramientas, h => Assert.True(h.SoloLectura));
    }
}
