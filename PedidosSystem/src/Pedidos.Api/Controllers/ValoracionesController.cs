using MediatR;
using Microsoft.AspNetCore.Mvc;
using Pedidos.Application.Valoraciones.Commands.CrearValoracion;
using Pedidos.Application.Valoraciones.Queries.ListarValoraciones;

namespace Pedidos.Api.Controllers;

/// <summary>Demo de Cosmos DB: valoraciones de productos, guardadas como documentos particionados por producto.</summary>
[ApiController]
[Route("api/cosmos/valoraciones")]
[Tags("Cosmos DB")]
public sealed class ValoracionesController(ISender sender) : ControllerBase
{
    /// <summary>Crea una valoración (puntaje de 1 a 5) de un producto.</summary>
    [HttpPost]
    [ProducesResponseType<Guid>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Crear(CrearValoracionCommand command, CancellationToken cancellationToken)
    {
        var id = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(Listar), new { productoId = command.ProductoId }, id);
    }

    /// <summary>Lista las valoraciones de un producto, con su cantidad y promedio.</summary>
    [HttpGet("{productoId}")]
    [ProducesResponseType<ValoracionesProductoDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(string productoId, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new ListarValoracionesQuery(productoId), cancellationToken));
}

