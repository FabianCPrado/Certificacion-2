using Clientes.Application.Clientes.Commands.ActualizarCliente;
using Clientes.Application.Clientes.Commands.CrearCliente;
using Clientes.Application.Clientes.Commands.EliminarCliente;
using Clientes.Application.Clientes.Queries.ListarClientes;
using Clientes.Application.Exceptions;
using Clientes.Application.Interfaces.Clientes;
using Clientes.Domain.Clientes;
using Clientes.Domain.ValueObjects;

namespace Clientes.Application.Tests;

internal sealed class FakeClienteRepository : IClienteRepository
{
    public List<Cliente> Guardados { get; } = [];

    public Task<Cliente?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Guardados.Find(c => c.Id == id));

    public Task<Cliente> CrearAsync(string nombre, Email email, CancellationToken cancellationToken = default)
    {
        var cliente = new Cliente { Nombre = nombre, Email = email.Valor };
        Guardados.Add(cliente);
        return Task.FromResult(cliente);
    }

    public Task ActualizarAsync(Cliente cliente, string nombre, Email email, CancellationToken cancellationToken = default)
    {
        cliente.Nombre = nombre;
        cliente.Email = email.Valor;
        return Task.CompletedTask;
    }

    public Task EliminarAsync(Cliente cliente, CancellationToken cancellationToken = default)
    {
        Guardados.Remove(cliente);
        return Task.CompletedTask;
    }
}

public class ClienteHandlersTests
{
    private readonly FakeClienteRepository _repositorio = new();

    [Fact]
    public async Task Crear_guarda_el_cliente_y_devuelve_su_id()
    {
        var id = await new CrearClienteCommandHandler(_repositorio)
            .Handle(new CrearClienteCommand("Ana", "ana@ejemplo.com"), CancellationToken.None);

        var cliente = Assert.Single(_repositorio.Guardados);
        Assert.Equal(cliente.Id, id);
        Assert.Equal("ana@ejemplo.com", cliente.Email);
    }

    [Fact]
    public async Task Actualizar_un_cliente_que_no_existe_lanza_NotFound()
    {
        var handler = new ActualizarClienteCommandHandler(_repositorio);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(
            new ActualizarClienteCommand(Guid.NewGuid(), "Ana", "ana@ejemplo.com"), CancellationToken.None));
    }

    [Fact]
    public async Task Eliminar_quita_el_cliente()
    {
        var id = await new CrearClienteCommandHandler(_repositorio)
            .Handle(new CrearClienteCommand("Ana", "ana@ejemplo.com"), CancellationToken.None);

        await new EliminarClienteCommandHandler(_repositorio)
            .Handle(new EliminarClienteCommand(id), CancellationToken.None);

        Assert.Empty(_repositorio.Guardados);
    }
}

public class ClienteValidatorsTests
{
    [Fact]
    public void Crear_con_email_invalido_es_invalido() =>
        Assert.False(new CrearClienteCommandValidator().Validate(new CrearClienteCommand("Ana", "no-es-email")).IsValid);

    [Fact]
    public void Listar_con_tamanio_fuera_de_rango_es_invalido() =>
        Assert.False(new ListarClientesQueryValidator().Validate(new ListarClientesQuery(1, 500)).IsValid);
}
