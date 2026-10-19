using MediatR;
using Clientes.Application.Exceptions;
using Clientes.Application.Interfaces.Clientes;
using Clientes.Domain.ValueObjects;
using Clientes.Application.Common;

namespace Clientes.Application.Clientes.Commands.ActualizarCliente;

public sealed record ActualizarClienteCommand(Guid ClienteId, string Nombre, string Email) : ICommand;

public sealed class ActualizarClienteCommandHandler(IClienteRepository clientes)
    : IRequestHandler<ActualizarClienteCommand>
{
    public async Task Handle(ActualizarClienteCommand request, CancellationToken cancellationToken)
    {
        var cliente = await clientes.ObtenerPorIdAsync(request.ClienteId, cancellationToken)
            ?? throw new NotFoundException($"No existe el cliente {request.ClienteId}.");

        await clientes.ActualizarAsync(cliente, request.Nombre, new Email(request.Email), cancellationToken);
    }
}
