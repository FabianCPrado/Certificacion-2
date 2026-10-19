using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Pedidos.Application.Interfaces.Pedidos;
using Pedidos.Application.Pedidos.Queries.VentasPorDia;
using Pedidos.Infrastructure.Persistence;

namespace Pedidos.Infrastructure.Queries.Pedidos;

/// <summary>
/// Reporte de ventas con Dapper puro: sin EF Core, solo la conexión a SQL Server. Resuelve la misma consulta de
/// dos formas, con el mismo resultado: con un procedimiento almacenado y con un SELECT más LINQ a objetos.
/// </summary>
public sealed class PedidosReportes(ConexionSql conexionSql) : IPedidosReportes
{
    // Una fila por línea de pedido confirmado: lo que trae el SELECT del método con LINQ
    private sealed record LineaVenta(DateTime CreadoEn, string Moneda, int Cantidad, decimal PrecioUnitario);

    public async Task<ReporteVentasDto> VentasPorDiaProcedimientoAsync(
        DateTime desde, DateTime hasta, CancellationToken cancellationToken = default)
    {
        // El procedimiento ObtenerVentasPorDia (scripts/procedimientos-clase4.sql) agrupa en la base.
        // Los parámetros viajan como parámetros reales del procedimiento, nunca dentro de un texto SQL.
        await using var conexion = new SqlConnection(conexionSql.CadenaConexion);
        var filas = await conexion.QueryAsync<VentaPorDiaDto>(new CommandDefinition(
            "ObtenerVentasPorDia",
            new { Desde = desde, Hasta = hasta },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken));

        var ventas = filas.ToList();
        return new ReporteVentasDto(ventas, ventas.Count);
    }

    public async Task<ReporteVentasDto> VentasPorDiaLinqAsync(
        DateTime desde, DateTime hasta, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT p.CreadoEn, p.Moneda, l.Cantidad, l.PrecioUnitario
            FROM Pedidos p
            JOIN LineasPedido l ON l.PedidoId = p.Id
            WHERE p.Estado = 'Confirmado' AND p.CreadoEn >= @desde AND p.CreadoEn < @hasta
            """;

        // Dapper solo trae las líneas (SELECT parametrizado)...
        await using var conexion = new SqlConnection(conexionSql.CadenaConexion);
        var lineas = (await conexion.QueryAsync<LineaVenta>(new CommandDefinition(
            sql, new { desde, hasta }, cancellationToken: cancellationToken))).ToList();

        // ...y LINQ a objetos agrupa y suma en memoria (aquí no hay Entity Framework: no se traduce a SQL).
        var ventas = lineas
            .GroupBy(l => (Dia: l.CreadoEn.Date, l.Moneda))
            .OrderBy(g => g.Key.Dia).ThenBy(g => g.Key.Moneda, StringComparer.Ordinal)
            .Select(g => new VentaPorDiaDto(g.Key.Dia, g.Key.Moneda, g.Sum(l => l.Cantidad * l.PrecioUnitario)))
            .ToList();

        return new ReporteVentasDto(ventas, lineas.Count);
    }
}
