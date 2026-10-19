using MediatR;
using Pedidos.Application.Common;
using Pedidos.Application.Interfaces.Valoraciones;

namespace Pedidos.Application.Valoraciones.Queries.ListarValoraciones;

/// <summary>Una valoración de un producto. Solo transporta datos: no tiene lógica.</summary>
public sealed record ValoracionDto(Guid Id, string ProductoId, string Usuario, int Puntaje, string? Comentario, DateTime FechaUtc);

/// <summary>Las valoraciones de un producto con su cantidad y promedio.</summary>
public sealed record ValoracionesProductoDto(
    string ProductoId, int Cantidad, double Promedio, IReadOnlyList<ValoracionDto> Valoraciones);

public sealed record ListarValoracionesQuery(string ProductoId) : IQuery<ValoracionesProductoDto>;

/// <summary>Consulta: solo usa <see cref="IValoracionConsultas"/> (lado de lectura), nunca el repositorio.</summary>
public sealed class ListarValoracionesQueryHandler(IValoracionConsultas consultas)
    : IRequestHandler<ListarValoracionesQuery, ValoracionesProductoDto>
{
    public async Task<ValoracionesProductoDto> Handle(ListarValoracionesQuery request, CancellationToken cancellationToken)
    {
        var valoraciones = await consultas.ListarPorProductoAsync(request.ProductoId, cancellationToken);
        var promedio = valoraciones.Count == 0 ? 0 : Math.Round(valoraciones.Average(v => v.Puntaje), 2);

        return new ValoracionesProductoDto(request.ProductoId, valoraciones.Count, promedio, valoraciones);
    }
}
