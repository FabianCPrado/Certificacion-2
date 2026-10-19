using MediatR;
using Clientes.Application.Exceptions;
using Clientes.Application.Interfaces.Clientes;
using Clientes.Application.Common;

namespace Clientes.Application.Clientes.Commands.EliminarCliente;

public sealed record EliminarClienteCommand(Guid ClienteId) : ICommand;

public sealed class EliminarClienteCommandHandler(IClienteRepository clientes)
    : IRequestHandler<EliminarClienteCommand>
{
    public async Task Handle(EliminarClienteCommand request, CancellationToken cancellationToken)
    {
        var cliente = await clientes.ObtenerPorIdAsync(request.ClienteId, cancellationToken)
            ?? throw new NotFoundException($"No existe el cliente {request.ClienteId}.");

        await clientes.EliminarAsync(cliente, cancellationToken);
    }
}
