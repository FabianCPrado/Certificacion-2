using Microsoft.EntityFrameworkCore;
using Pedidos.Application.Interfaces.Auditoria;

namespace Pedidos.Infrastructure.Persistence.Repositories.Auditoria;

public sealed class AuditoriaRepository(AppDbContext context, TimeProvider tiempo) : IAuditoriaRepository
{
    private const int MaxRuta = 300;
    private const int MaxUsuario = 100;
    private const int MaxTraceId = 100;

    public async Task RegistrarAsync(NuevaAuditoria auditoria, CancellationToken cancellationToken = default)
    {
        // INSERT directo, sin pasar por el change tracker: la auditoría corre al final del request y no
        // debe guardar por accidente cambios pendientes de la operación que se está auditando.
        await context.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO Auditoria (Id, FechaUtc, Metodo, Ruta, EstadoHttp, DuracionMs, Usuario, TraceId, Request)
            VALUES ({Guid.NewGuid()}, {tiempo.GetUtcNow().UtcDateTime}, {auditoria.Metodo},
                    {Recortar(auditoria.Ruta, MaxRuta)}, {auditoria.EstadoHttp}, {auditoria.DuracionMs},
                    {Recortar(auditoria.Usuario, MaxUsuario)}, {Recortar(auditoria.TraceId, MaxTraceId)},
                    {auditoria.Request})
            """,
            cancellationToken);
    }

    private static string Recortar(string valor, int maximo) =>
        valor.Length <= maximo ? valor : valor[..maximo];
}
