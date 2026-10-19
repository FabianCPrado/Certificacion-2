using Pedidos.Application.Exceptions;
using Pedidos.Application.Pedidos.Commands.CrearPedido;
using Pedidos.Domain.Common;

namespace Pedidos.Application.Tests;

public class CrearPedidoCommandHandlerTests
{
    private readonly FakePedidoRepository _repositorio = new();
    private readonly FakeCatalogoProductos _catalogo = new();

    private CrearPedidoCommandHandler CrearHandler() => new(_repositorio, _catalogo, TimeProvider.System);

    [Fact]
    public async Task Con_datos_validos_guarda_el_pedido_y_devuelve_su_id()
    {
        var id = await CrearHandler().Handle(Comandos.CrearPedidoValido(), CancellationToken.None);

        var pedido = Assert.Single(_repositorio.Guardados);
        Assert.Equal(pedido.Id, id);
        Assert.Equal(300_000m, pedido.Lineas.Sum(l => l.PrecioUnitario.Multiplicar(l.Cantidad).Monto));
        Assert.Equal(1, _repositorio.VecesGuardado);
    }

    [Fact]
    public async Task Con_email_invalido_lanza_excepcion_y_no_guarda()
    {
        await Assert.ThrowsAsync<ReglaNegocioException>(
            () => CrearHandler().Handle(Comandos.CrearPedidoValido("no-es-un-email"), CancellationToken.None));

        Assert.Empty(_repositorio.Guardados);
        Assert.Equal(0, _repositorio.VecesGuardado);
    }
}

public class CrearPedidoCommandValidatorTests
{
    private readonly CrearPedidoCommandValidator _validator = new();

    [Fact]
    public void Un_comando_valido_pasa_la_validacion()
    {
        Assert.True(_validator.Validate(Comandos.CrearPedidoValido()).IsValid);
    }

    [Fact]
    public void Sin_lineas_es_invalido()
    {
        var comando = Comandos.CrearPedidoValido() with { Lineas = [] };

        var resultado = _validator.Validate(comando);

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(CrearPedidoCommand.Lineas));
    }

    [Fact]
    public void Una_linea_con_cantidad_cero_es_invalida()
    {
        var comando = Comandos.CrearPedidoValido() with
        {
            Lineas = [new LineaRequest("p-1", "Electro", 0)]
        };

        Assert.False(_validator.Validate(comando).IsValid);
    }
}

public class CrearPedidoConProductoInexistenteTests
{
    [Fact]
    public async Task Si_el_producto_no_existe_en_el_catalogo_lanza_NotFound_y_no_guarda()
    {
        var repositorio = new FakePedidoRepository();
        var catalogo = new FakeCatalogoProductos();
        catalogo.Inexistentes.Add("p-1");
        var handler = new CrearPedidoCommandHandler(repositorio, catalogo, TimeProvider.System);

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(Comandos.CrearPedidoValido(), CancellationToken.None));

        Assert.Empty(repositorio.Guardados);
    }
}
