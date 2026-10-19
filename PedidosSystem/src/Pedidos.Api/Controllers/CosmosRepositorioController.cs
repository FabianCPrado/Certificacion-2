using Microsoft.AspNetCore.Mvc;
using Pedidos.Application.Interfaces.Catalogo;

namespace Pedidos.Api.Controllers;

/// <summary>Cosmos DB a través del repositorio (ICatalogoRepository), sin RU.</summary>
[ApiController]
[Route("api/cosmos/catalogo/repositorio")]
[Tags("Cosmos DB")]
public sealed class CosmosRepositorioController(ICatalogoRepository catalogo) : ControllerBase
{
    /// <summary>El catálogo de una categoría, leído con el repositorio.</summary>
    [HttpGet("{categoria}")]
    public async Task<IActionResult> PorCategoria(string categoria, CancellationToken cancellationToken) =>
        Ok(await catalogo.PorCategoriaAsync(categoria, cancellationToken));
}
