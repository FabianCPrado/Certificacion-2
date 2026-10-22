using FluentValidation;
using MediatR;
using Pedidos.Application.Common;
using Pedidos.Application.Interfaces.Pedidos;
using Pedidos.Application.Pedidos.Queries.ObtenerPedido;

namespace Pedidos.Application.Pedidos.Queries.ListarPedidosDetalle;

/// <summary>Un pedido para listados con detalle: incluye las líneas y no incluye el email del cliente.</summary>
public sealed record PedidoDetalleDto(
    Guid Id,
    Guid ClienteId,
    string Estado,
    decimal Total,
    string Moneda,
    DateTime CreadoEn,
    IReadOnlyList<LineaPedidoDto> Lineas);

public sealed record ListarPedidosDetalleQuery(int Pagina = 1, int Tamanio = 20, Guid? ClienteId = null)
    : IQuery<PagedResult<PedidoDetalleDto>>;

public sealed class ListarPedidosDetalleQueryValidator : AbstractValidator<ListarPedidosDetalleQuery>
{
    public ListarPedidosDetalleQueryValidator()
    {
        RuleFor(q => q.Pagina).GreaterThanOrEqualTo(1);
        RuleFor(q => q.Tamanio).InclusiveBetween(1, 100);
    }
}

public sealed class ListarPedidosDetalleQueryHandler(IPedidosConsultas consultas)
    : IRequestHandler<ListarPedidosDetalleQuery, PagedResult<PedidoDetalleDto>>
{
    public Task<PagedResult<PedidoDetalleDto>> Handle(ListarPedidosDetalleQuery request, CancellationToken cancellationToken) =>
        consultas.ListarDetalleAsync(request.Pagina, request.Tamanio, request.ClienteId, cancellationToken);
}
