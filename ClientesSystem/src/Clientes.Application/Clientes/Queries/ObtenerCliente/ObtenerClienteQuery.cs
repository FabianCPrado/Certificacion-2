using MediatR;
using Clientes.Application.Common;
using Clientes.Application.Exceptions;
using Clientes.Application.Interfaces.Clientes;

namespace Clientes.Application.Clientes.Queries.ObtenerCliente;

public sealed record ObtenerClienteQuery(Guid ClienteId) : IQuery<ClienteDto>;

/// <summary>Consulta: solo usa <see cref="IClienteConsultas"/> (lado de lectura), nunca el repositorio de escritura.</summary>
public sealed class ObtenerClienteQueryHandler(IClienteConsultas consultas)
    : IRequestHandler<ObtenerClienteQuery, ClienteDto>
{
    public async Task<ClienteDto> Handle(ObtenerClienteQuery request, CancellationToken cancellationToken) =>
        await consultas.ObtenerPorIdAsync(request.ClienteId, cancellationToken)
            ?? throw new NotFoundException($"No existe el cliente {request.ClienteId}.");
}
