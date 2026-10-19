using MediatR;
using Microsoft.AspNetCore.Mvc;
using Pedidos.Application.Auditoria.Queries.ListarAuditoria;
using Pedidos.Application.Common;

namespace Pedidos.Api.Controllers;

/// <summary>Consulta de la auditoría de las operaciones POST, PUT y DELETE. Solo lectura.</summary>
[ApiController]
[Route("api/auditoria")]
public sealed class AuditoriaController(ISender sender) : ControllerBase
{
    /// <summary>Lista los registros de auditoría (más recientes primero), con filtros por ruta y fechas.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<AuditoriaDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Listar(
        [FromQuery] string? ruta, [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta,
        CancellationToken cancellationToken, [FromQuery] int pagina = 1, [FromQuery] int tamanio = 20)
    {
        var registros = await sender.Send(new ListarAuditoriaQuery(pagina, tamanio, ruta, desde, hasta), cancellationToken);
        return Ok(registros);
    }
}
