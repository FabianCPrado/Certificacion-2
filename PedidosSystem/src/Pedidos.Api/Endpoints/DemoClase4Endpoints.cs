using System.Diagnostics;
using MediatR;
using Pedidos.Application.Interfaces.Demo;
using Pedidos.Application.Interfaces.Pedidos;
using Pedidos.Application.Pedidos.Commands.CancelarBorradoresAntiguos;
using Pedidos.Application.Pedidos.Commands.CrearPedido;
using Pedidos.Application.Pedidos.Commands.CrearPedidoConStock;
using Pedidos.Application.Pedidos.Queries.ListarPedidos;
using Pedidos.Application.Pedidos.Queries.VentasPorDia;

namespace Pedidos.Api.Endpoints;

/// <summary>
/// Endpoints de demo de la Clase 4 (EF Core, LINQ y Dapper). Cada tema tiene su versión "mala" y su versión
/// "buena" para comparar el SQL y los tiempos en la consola. Solo se mapean en Development.
/// Respetan la arquitectura: la versión buena pasa por los casos de uso y las consultas de Application, y la
/// mala por <see cref="IDemoMalasPracticas"/>; el código de acceso a datos de ambas vive en Infrastructure.
/// Requieren datos de prueba: scripts/seed-clase4.sql.
/// </summary>
public static class DemoClase4Endpoints
{
    private const int PedidosDemo = 100;

    // Todos los endpoints que usan Dapper se agrupan bajo esta etiqueta en Swagger
    private const string TagDapper = "Dapper";

    public static void MapDemoClase4(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/demo/clase4").WithTags("Demo Clase 4");

        MapConsultas(grupo);
        MapRendimiento(grupo);
        MapReporte(grupo);
        MapTransacciones(grupo);
        MapLinqVsDapper(grupo);
        MapDapper(grupo);
    }

    private static async Task<(ReporteVentasDto Reporte, long Milisegundos)> MedirAsync(
        Func<Task<ReporteVentasDto>> reporte)
    {
        var inicio = Stopwatch.GetTimestamp();
        var resultado = await reporte();
        return (resultado, (long)Stopwatch.GetElapsedTime(inicio).TotalMilliseconds);
    }

    // 4.6 · La MISMA consulta (ventas por día) con Dapper de las dos formas, juntas en una sola llamada
    private static void MapLinqVsDapper(RouteGroupBuilder g) =>
        g.MapGet("/ventas-por-dia", async (
            IPedidosReportes reportes, TimeProvider tiempo, CancellationToken ct, DateTime? desde, DateTime? hasta) =>
        {
            // Sin fechas, el último año hasta mañana
            var ahora = tiempo.GetUtcNow().UtcDateTime;
            var d = desde ?? ahora.AddYears(-1);
            var h = hasta ?? ahora.AddDays(1);

            // Una llamada de calentamiento a cada método (conexión, caché de Dapper), para comparar tiempos justos
            await reportes.VentasPorDiaProcedimientoAsync(d, h, ct);
            await reportes.VentasPorDiaLinqAsync(d, h, ct);

            var (conProcedimiento, msProcedimiento) = await MedirAsync(() => reportes.VentasPorDiaProcedimientoAsync(d, h, ct));
            var (conLinq, msLinq) = await MedirAsync(() => reportes.VentasPorDiaLinqAsync(d, h, ct));

            return Results.Ok(new
            {
                desde = d,
                hasta = h,
                iguales = conProcedimiento.Ventas.SequenceEqual(conLinq.Ventas),
                nota = "Tiempos medidos tras una llamada de calentamiento de cada método. Ninguno usa Entity Framework.",
                procedimiento = new
                {
                    tecnica = "Dapper · procedimiento almacenado ObtenerVentasPorDia (la base agrupa)",
                    milisegundos = msProcedimiento,
                    filasLeidasDeLaBase = conProcedimiento.FilasLeidas,
                    filasEnElResultado = conProcedimiento.Ventas.Count
                },
                linq = new
                {
                    tecnica = "Dapper · SELECT + LINQ a objetos (se agrupa en memoria)",
                    milisegundos = msLinq,
                    filasLeidasDeLaBase = conLinq.FilasLeidas,
                    filasEnElResultado = conLinq.Ventas.Count
                }
            });
        }).WithTags(TagDapper);

    // 4.6 · Dapper escribiendo: un procedimiento con parámetro de salida, para probarlo en clase
    private static void MapDapper(RouteGroupBuilder g)
    {
        var dapper = g.MapGroup("/dapper").WithTags(TagDapper);

        // ESCRITURA: Dapper ExecuteAsync con parámetro de salida (comando CancelarBorradoresAntiguos, vía repositorio)
        dapper.MapPost("/cancelar-borradores-antiguos", async (ISender sender, CancellationToken ct, int dias = 30) =>
        {
            var inicio = Stopwatch.GetTimestamp();
            var cancelados = await sender.Send(new CancelarBorradoresAntiguosCommand(dias), ct);
            var milisegundos = (long)Stopwatch.GetElapsedTime(inicio).TotalMilliseconds;

            return Results.Ok(new
            {
                tecnica = "Dapper · ExecuteAsync · procedimiento almacenado con parámetro de salida",
                procedimiento = "CancelarBorradoresAntiguos",
                dias,
                cancelados,
                milisegundos
            });
        });
    }

    // 4.4 · Transacciones: pedido con sus líneas + descuento de stock
    private static void MapTransacciones(RouteGroupBuilder g)
    {
        // MALO: guarda el pedido y después descuenta el stock; si falla una línea quedan datos a medias
        g.MapPost("/transaccion/malo", async (IDemoMalasPracticas malas, TimeProvider tiempo, CrearPedidoCommand pedido) =>
        {
            var id = await malas.CrearPedidoYDescontarStockSinTransaccionAsync(pedido, tiempo.GetUtcNow().UtcDateTime);
            return Results.Ok(new { id });
        });

        // BUENO: si falla una línea, se devuelve el stock ya descontado en el Catálogo (caso de uso CrearPedidoConStock)
        g.MapPost("/transaccion/bueno", async (ISender sender, CrearPedidoCommand pedido, CancellationToken ct) =>
        {
            var id = await sender.Send(new CrearPedidoConStockCommand(pedido), ct);
            return Results.Ok(new { id });
        });
    }

    // 4.4 · Consultas y proyecciones
    private static void MapConsultas(RouteGroupBuilder g)
    {
        // MALO: devolver la entidad completa, con sus líneas y con tracking
        g.MapGet("/pedidos/malo/{clienteId:guid}", async (IDemoMalasPracticas malas, Guid clienteId) =>
            await malas.PedidosDelClienteComoEntidadAsync(clienteId));

        // BUENO: AsNoTracking y proyección a DTO (caso de uso ListarPedidos)
        g.MapGet("/pedidos/bueno/{clienteId:guid}", async (ISender sender, Guid clienteId, CancellationToken ct) =>
            await sender.Send(new ListarPedidosQuery(1, 100, clienteId), ct));
    }

    // 4.5 · N+1
    private static void MapRendimiento(RouteGroupBuilder g)
    {
        // MALO: N+1 (1 consulta para los pedidos + 1 por cada pedido)
        g.MapGet("/n1/malo", async (IDemoMalasPracticas malas) =>
            await malas.PedidosConLineasN1Async(PedidosDemo));

        // BUENO: una sola consulta
        g.MapGet("/n1/bueno", async (IPedidosConsultas consultas, CancellationToken ct) =>
            await consultas.ListarConLineasAsync(PedidosDemo, ct));
    }

    // 4.6 · Dapper y la capa de datos
    private static void MapReporte(RouteGroupBuilder g)
    {
        // MALO: agrupar en memoria con EF Core, trayendo todos los pedidos con sus líneas
        g.MapGet("/reporte/malo", async (IDemoMalasPracticas malas) =>
            await malas.VentasPorDiaEnMemoriaAsync());

        // BUENO: Dapper detrás de IPedidosConsultas, la base agrupa
        g.MapGet("/reporte/bueno", async (IPedidosReportes reportes, DateTime desde, DateTime hasta, CancellationToken ct) =>
            (await reportes.VentasPorDiaProcedimientoAsync(desde, hasta, ct)).Ventas)
            .WithTags(TagDapper);
    }
}
