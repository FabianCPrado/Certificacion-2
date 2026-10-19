using FluentValidation;
using MediatR;
using Clientes.Application.Clientes.Queries.ObtenerCliente;
using Clientes.Application.Common;
using Clientes.Application.Interfaces.Clientes;

namespace Clientes.Application.Clientes.Queries.ListarClientes;

public sealed record ListarClientesQuery(int Pagina = 1, int Tamanio = 20) : IQuery<PagedResult<ClienteDto>>;

public sealed class ListarClientesQueryValidator : AbstractValidator<ListarClientesQuery>
{
    public ListarClientesQueryValidator()
    {
        RuleFor(q => q.Pagina).GreaterThanOrEqualTo(1);
        RuleFor(q => q.Tamanio).InclusiveBetween(1, 100);
    }
}

public sealed class ListarClientesQueryHandler(IClienteConsultas consultas)
    : IRequestHandler<ListarClientesQuery, PagedResult<ClienteDto>>
{
    public Task<PagedResult<ClienteDto>> Handle(ListarClientesQuery request, CancellationToken cancellationToken) =>
        consultas.ListarAsync(request.Pagina, request.Tamanio, cancellationToken);
}
