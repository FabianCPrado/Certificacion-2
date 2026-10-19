using MediatR;
using Microsoft.AspNetCore.Mvc;
using Pedidos.Application.Clientes.Queries.ObtenerCliente;
using Pedidos.Application.Common;
using Pedidos.Application.Pedidos.Commands.ActualizarPedido;
using Pedidos.Application.Pedidos.Commands.CancelarBorradoresAntiguos;
using Pedidos.Application.Pedidos.Commands.CancelarPedido;
using Pedidos.Application.Pedidos.Commands.ConfirmarPedido;
using Pedidos.Application.Pedidos.Commands.CrearPedido;
using Pedidos.Application.Pedidos.Commands.EliminarPedido;
using Pedidos.Application.Pedidos.Queries.ListarPedidos;
using Pedidos.Application.Pedidos.Queries.ObtenerClienteDePedido;
using Pedidos.Application.Pedidos.Queries.ObtenerPedido;

namespace Pedidos.Api.Controllers;

/// <summary>
/// El controlador no tiene lógica: envía cada request por MediatR
/// (validación → handler) y devuelve la respuesta HTTP.
/// Los errores los traduce <see cref="Infrastructure.GlobalExceptionHandler"/>.
/// </summary>
[ApiController]
[Route("api/pedidos")]
public sealed class PedidosController(ISender sender) : ControllerBase
{
    /// <summary>Crea un pedido en estado borrador.</summary>
    [HttpPost]
    [ProducesResponseType<Guid>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Crear(CrearPedidoCommand command, CancellationToken cancellationToken)
    {
        var id = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(Obtener), new { id }, id);
    }

    /// <summary>Obtiene un pedido con sus líneas.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<PedidoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(Guid id, CancellationToken cancellationToken)
    {
        var pedido = await sender.Send(new ObtenerPedidoQuery(id), cancellationToken);
        return Ok(pedido);
    }

    /// <summary>Obtiene los datos del cliente de un pedido, consultándolos al momento al servicio Clientes (REST).</summary>
    [HttpGet("{id:guid}/cliente")]
    [ProducesResponseType<ClienteDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> ObtenerCliente(Guid id, CancellationToken cancellationToken)
    {
        var cliente = await sender.Send(new ObtenerClienteDePedidoQuery(id), cancellationToken);
        return Ok(cliente);
    }

    /// <summary>Lista los pedidos (resumen, paginado), opcionalmente de un solo cliente.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<PedidoResumenDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Listar(
        [FromQuery] Guid? clienteId, CancellationToken cancellationToken, [FromQuery] int pagina = 1, [FromQuery] int tamanio = 20)
    {
        var pedidos = await sender.Send(new ListarPedidosQuery(pagina, tamanio, clienteId), cancellationToken);
        return Ok(pedidos);
    }

    /// <summary>Cambia el email y la dirección de envío de un pedido en borrador.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Actualizar(
        Guid id, ActualizarPedidoRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new ActualizarPedidoCommand(id, request.EmailCliente, request.Direccion), cancellationToken);
        return NoContent();
    }

    /// <summary>Elimina un pedido en borrador.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new EliminarPedidoCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>Cancela los pedidos en borrador con más de N días sin confirmarse. Devuelve cuántos canceló.</summary>
    [HttpPost("cancelar-borradores-antiguos")]
    [ProducesResponseType<int>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelarBorradoresAntiguos(
        CancellationToken cancellationToken, [FromQuery] int dias = 30)
    {
        var cancelados = await sender.Send(new CancelarBorradoresAntiguosCommand(dias), cancellationToken);
        return Ok(new { cancelados });
    }

    /// <summary>Confirma un pedido en borrador.</summary>
    [HttpPost("{id:guid}/confirmar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Confirmar(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new ConfirmarPedidoCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>Cancela un pedido.</summary>
    [HttpPost("{id:guid}/cancelar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancelar(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new CancelarPedidoCommand(id), cancellationToken);
        return NoContent();
    }
}

/// <summary>Cuerpo de PUT /api/pedidos/{id}: el Id viaja en la ruta.</summary>
public sealed record ActualizarPedidoRequest(string EmailCliente, DireccionRequest Direccion);
