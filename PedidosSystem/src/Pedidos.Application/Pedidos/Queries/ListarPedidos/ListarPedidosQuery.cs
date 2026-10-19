using FluentValidation;
using MediatR;
using Pedidos.Application.Common;
using Pedidos.Application.Interfaces.Pedidos;

namespace Pedidos.Application.Pedidos.Queries.ListarPedidos;

public sealed record ListarPedidosQuery(int Pagina = 1, int Tamanio = 20, Guid? ClienteId = null)
    : IQuery<PagedResult<PedidoResumenDto>>;

public sealed class ListarPedidosQueryValidator : AbstractValidator<ListarPedidosQuery>
{
    public ListarPedidosQueryValidator()
    {
        RuleFor(q => q.Pagina).GreaterThanOrEqualTo(1);
        RuleFor(q => q.Tamanio).InclusiveBetween(1, 100);
    }
}

public sealed class ListarPedidosQueryHandler(IPedidosConsultas consultas)
    : IRequestHandler<ListarPedidosQuery, PagedResult<PedidoResumenDto>>
{
    public Task<PagedResult<PedidoResumenDto>> Handle(ListarPedidosQuery request, CancellationToken cancellationToken) =>
        consultas.ListarAsync(request.Pagina, request.Tamanio, request.ClienteId, cancellationToken);
}
