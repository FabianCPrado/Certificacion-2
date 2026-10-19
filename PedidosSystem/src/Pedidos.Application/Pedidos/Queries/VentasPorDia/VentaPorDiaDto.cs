namespace Pedidos.Application.Pedidos.Queries.VentasPorDia;

/// <summary>Total de pedidos confirmados de un día, por moneda. Solo transporta datos.</summary>
public sealed record VentaPorDiaDto(DateTime Dia, string Moneda, decimal Total);

/// <summary>Resultado del reporte de ventas y cuántas filas se leyeron de la base para obtenerlo.</summary>
public sealed record ReporteVentasDto(IReadOnlyList<VentaPorDiaDto> Ventas, int FilasLeidas);
