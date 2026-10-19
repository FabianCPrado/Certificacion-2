using Microsoft.AspNetCore.Mvc;
using Pedidos.Application.Interfaces.Demo;

namespace Pedidos.Api.Controllers;

/// <summary>Cosmos DB con el proveedor de EF Core: el mismo catálogo con DbContext y LINQ.</summary>
[ApiController]
[Route("api/cosmos/ef")]
[Tags("Cosmos DB")]
public sealed class CosmosEfController(IDemoCatalogoEf demoEf) : ControllerBase
{
    /// <summary>Carga 30 productos de prueba con EF Core (AddRange + SaveChanges).</summary>
    [HttpPost("seed")]
    public async Task<IActionResult> Cargar(CancellationToken cancellationToken) =>
        Ok(new { cargados = await demoEf.CargarAsync(cancellationToken) });

    /// <summary>Lista los productos de una categoría (Bebidas o Electro) con LINQ y WithPartitionKey.</summary>
    [HttpGet("{categoria}")]
    public async Task<IActionResult> Listar(string categoria, CancellationToken cancellationToken) =>
        Ok(await demoEf.ListarAsync(categoria, cancellationToken));
}
