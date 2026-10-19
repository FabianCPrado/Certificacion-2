using Microsoft.EntityFrameworkCore;
using Pedidos.Domain.Common;
using Pedidos.Domain.Pedidos;
using Pedidos.Domain.ValueObjects;
using Pedidos.Infrastructure.Persistence;
using Pedidos.Infrastructure.Persistence.Repositories.Pedidos;

namespace Pedidos.Infrastructure.Tests;

/// <summary>Reglas de negocio implementadas en el repositorio. No abre conexión: solo usa el change tracker.</summary>
public class PedidoRepositoryReglasTests
{
    private readonly PedidoRepository _repositorio = new(new AppDbContext(
        new DbContextOptionsBuilder<AppDbContext>().UseSqlServer("Server=.;Database=Pruebas").Options));

    private Pedido NuevoPedido() => _repositorio.Crear(
        Guid.NewGuid(),
        new Email("ana@ejemplo.com"),
        new Direccion("Calle 100 # 15-20", "Bogotá", "110111", "Colombia"),
        "COP",
        DateTime.UtcNow);

    private static Dinero Pesos(decimal monto) => new(monto, "COP");

    [Fact]
    public void Un_pedido_nuevo_queda_en_borrador()
    {
        var pedido = NuevoPedido();

        Assert.Equal(EstadoPedido.Borrador, pedido.Estado);
        Assert.Empty(pedido.Lineas);
    }

    [Fact]
    public void Crear_sin_cliente_lanza_ReglaNegocioException()
    {
        Assert.Throws<ReglaNegocioException>(() => _repositorio.Crear(
            Guid.Empty, new Email("ana@ejemplo.com"), new Direccion("Calle 1", "Bogotá", "", "Colombia"), "COP", DateTime.UtcNow));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void AgregarLinea_con_cantidad_no_positiva_lanza_excepcion(int cantidad)
    {
        var pedido = NuevoPedido();

        Assert.Throws<ReglaNegocioException>(() => _repositorio.AgregarLinea(pedido, Guid.NewGuid().ToString(), "Electro", "Teclado", Pesos(100), cantidad));
        Assert.Empty(pedido.Lineas);
    }

    [Fact]
    public void AgregarLinea_en_otra_moneda_lanza_excepcion()
    {
        var pedido = NuevoPedido();

        Assert.Throws<ReglaNegocioException>(
            () => _repositorio.AgregarLinea(pedido, Guid.NewGuid().ToString(), "Electro", "Teclado", new Dinero(10, "USD"), 1));
    }

    [Fact]
    public void AgregarLinea_del_mismo_producto_suma_la_cantidad()
    {
        var pedido = NuevoPedido();
        var productoId = Guid.NewGuid().ToString();

        _repositorio.AgregarLinea(pedido, productoId, "Electro", "Teclado", Pesos(100), 2);
        _repositorio.AgregarLinea(pedido, productoId, "Electro", "Teclado", Pesos(100), 3);

        var linea = Assert.Single(pedido.Lineas);
        Assert.Equal(5, linea.Cantidad);
    }

    [Fact]
    public void Confirmar_un_pedido_sin_lineas_lanza_excepcion()
    {
        var pedido = NuevoPedido();

        Assert.Throws<ReglaNegocioException>(() => _repositorio.Confirmar(pedido));
        Assert.Equal(EstadoPedido.Borrador, pedido.Estado);
    }

    [Fact]
    public void Confirmar_cambia_el_estado_a_Confirmado()
    {
        var pedido = NuevoPedido();
        _repositorio.AgregarLinea(pedido, Guid.NewGuid().ToString(), "Electro", "Teclado", Pesos(100), 1);

        _repositorio.Confirmar(pedido);

        Assert.Equal(EstadoPedido.Confirmado, pedido.Estado);
    }

    [Fact]
    public void Un_pedido_confirmado_no_admite_nuevas_lineas()
    {
        var pedido = NuevoPedido();
        _repositorio.AgregarLinea(pedido, Guid.NewGuid().ToString(), "Electro", "Teclado", Pesos(100), 1);
        _repositorio.Confirmar(pedido);

        Assert.Throws<ReglaNegocioException>(
            () => _repositorio.AgregarLinea(pedido, Guid.NewGuid().ToString(), "Electro", "Mouse", Pesos(50), 1));
    }

    [Fact]
    public void Cancelar_dos_veces_lanza_excepcion()
    {
        var pedido = NuevoPedido();
        _repositorio.Cancelar(pedido);

        Assert.Throws<ReglaNegocioException>(() => _repositorio.Cancelar(pedido));
    }
}
