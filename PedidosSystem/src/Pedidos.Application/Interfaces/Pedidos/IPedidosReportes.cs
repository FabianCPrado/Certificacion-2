using Pedidos.Application.Pedidos.Queries.VentasPorDia;

namespace Pedidos.Application.Interfaces.Pedidos;

/// <summary>
/// Reporte de ventas de pedidos, con Dapper puro (sin EF Core). La define Application e Infrastructure la
/// implementa de dos formas, con el mismo resultado, para compararlas.
/// </summary>
public interface IPedidosReportes
{
    /// <summary>Dapper llama a un procedimiento almacenado: la base agrupa y devuelve solo el resultado.</summary>
    Task<ReporteVentasDto> VentasPorDiaProcedimientoAsync(
        DateTime desde, DateTime hasta, CancellationToken cancellationToken = default);

    /// <summary>Dapper trae las líneas con un SELECT y LINQ (a objetos) agrupa y suma en memoria.</summary>
    Task<ReporteVentasDto> VentasPorDiaLinqAsync(
        DateTime desde, DateTime hasta, CancellationToken cancellationToken = default);
}
