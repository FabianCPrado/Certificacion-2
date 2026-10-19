using MediatR;
using Microsoft.AspNetCore.Mvc;
using Pedidos.Application.Clientes.Queries.ListarClientes;
using Pedidos.Application.Clientes.Queries.ObtenerCliente;
using Pedidos.Application.Common;

namespace Pedidos.Api.Controllers;

/// <summary>
/// Solo lectura: los clientes pertenecen al servicio Clientes (ClientesSystem), que es el único que los crea,
/// cambia o elimina. Aquí se consultan al momento, por REST, cuando se necesitan.
/// El controlador no tiene lógica: envía cada request por MediatR y devuelve la respuesta HTTP.
/// </summary>
[ApiController]
[Route("api/clientes")]
public sealed class ClientesController(ISender sender) : ControllerBase
{
    /// <summary>Lista los clientes (paginado).</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<ClienteDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Listar(
        CancellationToken cancellationToken, [FromQuery] int pagina = 1, [FromQuery] int tamanio = 20)
    {
        var clientes = await sender.Send(new ListarClientesQuery(pagina, tamanio), cancellationToken);
        return Ok(clientes);
    }

    /// <summary>Obtiene un cliente.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<ClienteDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Obtener(Guid id, CancellationToken cancellationToken)
    {
        var cliente = await sender.Send(new ObtenerClienteQuery(id), cancellationToken);
        return Ok(cliente);
    }
}
