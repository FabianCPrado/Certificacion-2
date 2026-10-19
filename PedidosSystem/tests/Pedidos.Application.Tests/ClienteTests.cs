using Pedidos.Application.Clientes.Queries.ListarClientes;
using Pedidos.Application.Clientes.Queries.ObtenerCliente;
using Pedidos.Application.Common;
using Pedidos.Application.Exceptions;
using Pedidos.Application.Interfaces.Clientes;
using Pedidos.Application.Interfaces.Pedidos;
using Pedidos.Application.Pedidos.Queries.ListarPedidos;
using Pedidos.Application.Pedidos.Queries.ObtenerClienteDePedido;
using Pedidos.Application.Pedidos.Queries.ObtenerPedido;

namespace Pedidos.Application.Tests;

internal sealed class FakeClienteConsultas(params ClienteDto[] clientes) : IClienteConsultas
{
    public Task<ClienteDto?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(clientes.FirstOrDefault(c => c.Id == id));

    public Task<PagedResult<ClienteDto>> ListarAsync(int pagina, int tamanio, CancellationToken cancellationToken = default) =>
        Task.FromResult(new PagedResult<ClienteDto>(clientes, pagina, tamanio, clientes.Length));
}

internal sealed class FakePedidosConsultas(params PedidoDto[] pedidos) : IPedidosConsultas
{
    public Task<PedidoDto?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(pedidos.FirstOrDefault(p => p.Id == id));

    public Task<PagedResult<PedidoResumenDto>> ListarAsync(
        int pagina, int tamanio, Guid? clienteId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<IReadOnlyList<PedidoDto>> ListarPorFechasAsync(
        DateOnly desde, DateOnly hasta, int maximo, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<IReadOnlyList<PedidoConLineasDto>> ListarConLineasAsync(
        int cantidad, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}

public class ObtenerClienteDePedidoTests
{
    private static PedidoDto Pedido(Guid clienteId) =>
        new(Guid.NewGuid(), clienteId, "ana@ejemplo.com", "Borrador", 0m, "COP", DateTime.UtcNow, []);

    [Fact]
    public async Task Devuelve_el_cliente_que_tiene_el_servicio_Clientes()
    {
        var cliente = new ClienteDto(Guid.NewGuid(), "Ana", "ana@ejemplo.com");
        var pedido = Pedido(cliente.Id);
        var handler = new ObtenerClienteDePedidoQueryHandler(new FakePedidosConsultas(pedido), new FakeClienteConsultas(cliente));

        var resultado = await handler.Handle(new ObtenerClienteDePedidoQuery(pedido.Id), CancellationToken.None);

        Assert.Equal(cliente, resultado);
    }

    [Fact]
    public async Task Un_pedido_que_no_existe_lanza_NotFound()
    {
        var handler = new ObtenerClienteDePedidoQueryHandler(new FakePedidosConsultas(), new FakeClienteConsultas());

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new ObtenerClienteDePedidoQuery(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Un_cliente_que_ya_no_existe_en_Clientes_lanza_NotFound()
    {
        var pedido = Pedido(Guid.NewGuid());
        var handler = new ObtenerClienteDePedidoQueryHandler(new FakePedidosConsultas(pedido), new FakeClienteConsultas());

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new ObtenerClienteDePedidoQuery(pedido.Id), CancellationToken.None));
    }
}

public class ClienteValidatorsTests
{
    [Fact]
    public void Listar_con_tamanio_fuera_de_rango_es_invalido() =>
        Assert.False(new ListarClientesQueryValidator().Validate(new ListarClientesQuery(1, 500)).IsValid);

    [Fact]
    public void Listar_con_pagina_y_tamanio_validos_es_valido() =>
        Assert.True(new ListarClientesQueryValidator().Validate(new ListarClientesQuery(1, 20)).IsValid);
}
