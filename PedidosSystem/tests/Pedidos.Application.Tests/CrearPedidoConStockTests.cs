using Microsoft.Extensions.Logging.Abstractions;
using Pedidos.Application.Pedidos.Commands.CrearPedido;
using Pedidos.Application.Pedidos.Commands.CrearPedidoConStock;
using Pedidos.Domain.Common;

namespace Pedidos.Application.Tests;

public class CrearPedidoConStockCommandHandlerTests
{
    private readonly FakePedidoRepository _repositorio = new();
    private readonly FakeCatalogoProductos _catalogo = new();

    private CrearPedidoConStockCommandHandler CrearHandler() =>
        new(_repositorio, _catalogo, TimeProvider.System, NullLogger<CrearPedidoConStockCommandHandler>.Instance);

    [Fact]
    public async Task Descuenta_el_stock_de_cada_linea_y_guarda_el_pedido()
    {
        var id = await CrearHandler().Handle(new CrearPedidoConStockCommand(Comandos.CrearPedidoValido()), CancellationToken.None);

        Assert.Equal(Assert.Single(_repositorio.Guardados).Id, id);
        Assert.Equal(1, _catalogo.Descuentos);
        Assert.Equal(0, _catalogo.Reposiciones);
    }

    [Fact]
    public async Task Si_no_alcanza_el_stock_de_una_linea_devuelve_el_de_las_anteriores_y_no_guarda()
    {
        _catalogo.Stock["p-2"] = 1;
        var pedido = Comandos.CrearPedidoValido() with
        {
            Lineas = [new LineaRequest("p-1", "Electro", 2), new LineaRequest("p-2", "Electro", 5)]
        };

        await Assert.ThrowsAsync<ReglaNegocioException>(
            () => CrearHandler().Handle(new CrearPedidoConStockCommand(pedido), CancellationToken.None));

        Assert.Equal(1, _catalogo.Descuentos);      // se descontó p-1
        Assert.Equal(1, _catalogo.Reposiciones);    // y se devolvió
        Assert.Empty(_repositorio.Guardados);
    }
}
