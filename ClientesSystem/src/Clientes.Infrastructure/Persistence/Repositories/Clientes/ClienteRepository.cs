using Microsoft.EntityFrameworkCore;
using Clientes.Application.Interfaces.Clientes;
using Clientes.Application.Interfaces.Pedidos;
using Clientes.Domain.Clientes;
using Clientes.Domain.Common;
using Clientes.Domain.ValueObjects;

namespace Clientes.Infrastructure.Persistence.Repositories.Clientes;

public sealed class ClienteRepository(AppDbContext context, IPedidosConsultas pedidos) : IClienteRepository
{
    public Task<Cliente?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Clientes.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<Cliente> CrearAsync(string nombre, Email email, CancellationToken cancellationToken = default)
    {
        ValidarNombre(nombre);
        await ValidarEmailDisponibleAsync(email, idActual: null, cancellationToken);

        var cliente = new Cliente { Nombre = nombre.Trim(), Email = email.Valor };
        context.Clientes.Add(cliente);

        await context.SaveChangesAsync(cancellationToken);
        return cliente;
    }

    public async Task ActualizarAsync(
        Cliente cliente, string nombre, Email email, CancellationToken cancellationToken = default)
    {
        ValidarNombre(nombre);
        await ValidarEmailDisponibleAsync(email, cliente.Id, cancellationToken);

        cliente.Nombre = nombre.Trim();
        cliente.Email = email.Valor;

        await context.SaveChangesAsync(cancellationToken);
    }

    // Los pedidos son del servicio Pedidos: se le pregunta por REST. Si no responde, no se borra (falla cerrado → 503).
    public async Task EliminarAsync(Cliente cliente, CancellationToken cancellationToken = default)
    {
        if (await pedidos.ClienteTienePedidosAsync(cliente.Id, cancellationToken))
            throw new ReglaNegocioException("No se puede eliminar un cliente que tiene pedidos.");

        context.Clientes.Remove(cliente);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static void ValidarNombre(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ReglaNegocioException("El nombre del cliente es obligatorio.");
    }

    private async Task ValidarEmailDisponibleAsync(Email email, Guid? idActual, CancellationToken cancellationToken)
    {
        var existe = await context.Clientes
            .AnyAsync(c => c.Email == email.Valor && c.Id != idActual, cancellationToken);

        if (existe)
            throw new ReglaNegocioException("Ya existe un cliente con ese email.");
    }
}
