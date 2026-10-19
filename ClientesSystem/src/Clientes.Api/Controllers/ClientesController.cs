using MediatR;
using Microsoft.AspNetCore.Mvc;
using Clientes.Application.Clientes.Commands.ActualizarCliente;
using Clientes.Application.Clientes.Commands.CrearCliente;
using Clientes.Application.Clientes.Commands.EliminarCliente;
using Clientes.Application.Clientes.Queries.ListarClientes;
using Clientes.Application.Clientes.Queries.ObtenerCliente;
using Clientes.Application.Clientes.Queries.PedidosRedis;
using Clientes.Application.Common;
using Clientes.Application.Interfaces.Pedidos;

namespace Clientes.Api.Controllers;

/// <summary>
/// El controlador no tiene lógica: envía cada request por MediatR (validación → handler)
/// y devuelve la respuesta HTTP. Los errores los traduce <see cref="Infrastructure.GlobalExceptionHandler"/>.
/// </summary>
[ApiController]
[Route("api/clientes")]
public sealed class ClientesController(ISender sender) : ControllerBase
{
    /// <summary>Crea un cliente.</summary>
    [HttpPost]
    [ProducesResponseType<Guid>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Crear(CrearClienteCommand command, CancellationToken cancellationToken)
    {
        var id = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(Obtener), new { id }, id);
    }

    /// <summary>Lista los clientes (paginado).</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<ClienteDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
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
    public async Task<IActionResult> Obtener(Guid id, CancellationToken cancellationToken)
    {
        var cliente = await sender.Send(new ObtenerClienteQuery(id), cancellationToken);
        return Ok(cliente);
    }

    /// <summary>Cambia el nombre y el email de un cliente.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Actualizar(
        Guid id, ActualizarClienteRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new ActualizarClienteCommand(id, request.Nombre, request.Email), cancellationToken);
        return NoContent();
    }

    /// <summary>Elimina un cliente que no tenga pedidos.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new EliminarClienteCommand(id), cancellationToken);
        return NoContent();
    }
}

/// <summary>Demo de Redis desde Clientes: consume la API de Pedidos, que es quien tiene el caché.</summary>
[ApiController]
[Route("api/redis")]
public sealed class RedisController(ISender sender) : ControllerBase
{
    /// <summary>Pedidos entre dos fechas, pedidos a Pedidos. "origen" dice si salieron de "redis" o de "sql".</summary>
    [HttpGet("pedidos")]
    [ProducesResponseType<PedidosPorFechasDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ListarPedidosPorFechas(
        [FromQuery] DateOnly desde, [FromQuery] DateOnly hasta, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new ObtenerPedidosRedisQuery(desde, hasta), cancellationToken));
}

/// <summary>Cuerpo de PUT /api/clientes/{id}: el Id viaja en la ruta.</summary>
public sealed record ActualizarClienteRequest(string Nombre, string Email);

