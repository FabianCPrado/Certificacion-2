using MediatR;
using Clientes.Application.Interfaces.Clientes;
using Clientes.Domain.ValueObjects;
using Clientes.Application.Common;

namespace Clientes.Application.Clientes.Commands.CrearCliente;

/// <summary>Datos para crear un cliente. Devuelve el Id del cliente creado.</summary>
public sealed record CrearClienteCommand(string Nombre, string Email) : ICommand<Guid>;

public sealed class CrearClienteCommandHandler(IClienteRepository clientes)
    : IRequestHandler<CrearClienteCommand, Guid>
{
    public async Task<Guid> Handle(CrearClienteCommand request, CancellationToken cancellationToken)
    {
        var cliente = await clientes.CrearAsync(request.Nombre, new Email(request.Email), cancellationToken);
        return cliente.Id;
    }
}
