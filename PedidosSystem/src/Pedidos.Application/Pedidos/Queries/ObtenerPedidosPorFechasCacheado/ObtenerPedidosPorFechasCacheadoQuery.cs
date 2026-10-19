using System.Diagnostics;
using FluentValidation;
using MediatR;
using Pedidos.Application.Common;
using Pedidos.Application.Interfaces.Common;
using Pedidos.Application.Interfaces.Pedidos;
using Pedidos.Application.Pedidos.Queries.ObtenerPedido;

namespace Pedidos.Application.Pedidos.Queries.ObtenerPedidosPorFechasCacheado;

public sealed record ObtenerPedidosPorFechasCacheadoQuery(DateOnly Desde, DateOnly Hasta)
    : IQuery<PedidosPorFechasCacheadosDto>;

/// <summary>Los pedidos con sus productos, de dónde salieron ("redis" o "sql") y cuánto tardó.</summary>
public sealed record PedidosPorFechasCacheadosDto(
    IReadOnlyList<PedidoDto> Pedidos, int Cantidad, string Origen, double Milisegundos);

public sealed class ObtenerPedidosPorFechasCacheadoQueryValidator : AbstractValidator<ObtenerPedidosPorFechasCacheadoQuery>
{
    public ObtenerPedidosPorFechasCacheadoQueryValidator()
    {
        RuleFor(q => q.Hasta).GreaterThanOrEqualTo(q => q.Desde)
            .WithMessage("'hasta' no puede ser anterior a 'desde'.");

        RuleFor(q => q).Must(q => q.Hasta.DayNumber - q.Desde.DayNumber < ObtenerPedidosPorFechasCacheadoQueryHandler.MaximoDias)
            .WithName("Rango")
            .WithMessage($"El rango no puede superar {ObtenerPedidosPorFechasCacheadoQueryHandler.MaximoDias} días.");
    }
}

/// <summary>Cache-aside: un rango de fechas es una clave; si está en Redis no se toca SQL.</summary>
public sealed class ObtenerPedidosPorFechasCacheadoQueryHandler(IPedidosConsultas consultas, ICacheService cache)
    : IRequestHandler<ObtenerPedidosPorFechasCacheadoQuery, PedidosPorFechasCacheadosDto>
{
    public const int MaximoDias = 31;
    private const int MaximoPedidos = 200;
    private static readonly TimeSpan Duracion = TimeSpan.FromMinutes(5);

    public async Task<PedidosPorFechasCacheadosDto> Handle(
        ObtenerPedidosPorFechasCacheadoQuery request, CancellationToken cancellationToken)
    {
        var reloj = Stopwatch.StartNew();
        var clave = ClavesCache.PedidosPorFechas(request.Desde, request.Hasta);

        var enCache = await cache.ObtenerAsync<List<PedidoDto>>(clave, cancellationToken);
        if (enCache is not null)
            return new PedidosPorFechasCacheadosDto(enCache, enCache.Count, "redis", reloj.Elapsed.TotalMilliseconds);

        var pedidos = await consultas.ListarPorFechasAsync(request.Desde, request.Hasta, MaximoPedidos, cancellationToken);

        await cache.GuardarAsync(clave, pedidos, Duracion, cancellationToken);
        return new PedidosPorFechasCacheadosDto(pedidos, pedidos.Count, "sql", reloj.Elapsed.TotalMilliseconds);
    }
}
