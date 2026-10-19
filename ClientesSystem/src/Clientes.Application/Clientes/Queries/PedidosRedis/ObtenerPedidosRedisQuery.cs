using FluentValidation;
using MediatR;
using Clientes.Application.Common;
using Clientes.Application.Interfaces.Pedidos;

namespace Clientes.Application.Clientes.Queries.PedidosRedis;

public sealed record ObtenerPedidosRedisQuery(DateOnly Desde, DateOnly Hasta) : IQuery<PedidosPorFechasDto>;

public sealed class ObtenerPedidosRedisQueryValidator : AbstractValidator<ObtenerPedidosRedisQuery>
{
    public ObtenerPedidosRedisQueryValidator()
    {
        RuleFor(q => q.Hasta).GreaterThanOrEqualTo(q => q.Desde)
            .WithMessage("'hasta' no puede ser anterior a 'desde'.");
    }
}

/// <summary>La misma consulta de Pedidos, vista desde Clientes: aquí no hay Redis, solo una llamada al otro servicio.</summary>
public sealed class ObtenerPedidosRedisQueryHandler(IPedidosConsultas pedidos)
    : IRequestHandler<ObtenerPedidosRedisQuery, PedidosPorFechasDto>
{
    public Task<PedidosPorFechasDto> Handle(ObtenerPedidosRedisQuery request, CancellationToken cancellationToken) =>
        pedidos.ListarPorFechasAsync(request.Desde, request.Hasta, cancellationToken);
}
