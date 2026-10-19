using Microsoft.AspNetCore.Mvc;
using Pedidos.Application.Interfaces.Demo;

namespace Pedidos.Api.Controllers;

/// <summary>Cosmos DB con el SDK: lectura puntual, consultas buenas y malas, y consulta entre particiones.</summary>
[ApiController]
[Route("api/cosmos/catalogo")]
[Tags("Cosmos DB")]
public sealed class CosmosController(IDemoCatalogo demo) : ControllerBase
{
    /// <summary>Carga productos de prueba en el catálogo (SDK).</summary>
    [HttpPost("seed")]
    public async Task<IActionResult> Cargar(CancellationToken cancellationToken) =>
        Ok(new { cargados = await demo.CargarAsync(cancellationToken) });

    /// <summary>Lectura puntual: id + clave de partición (la categoría).</summary>
    [HttpGet("{categoria}/{id}")]
    public async Task<IActionResult> LeerPuntual(string categoria, string id, CancellationToken cancellationToken) =>
        await demo.LeerPuntualAsync(categoria, id, cancellationToken) is { } lectura ? Ok(lectura) : NotFound();

    /// <summary>
    /// LO MALO, A PROPÓSITO: la consulta se arma concatenando texto (probar con categoria=' OR '1'='1).
    /// Solo responde en Development: es solo para la clase y no debe quedar disponible en otros ambientes.
    /// </summary>
    [HttpGet("malo")]
    public async Task<IActionResult> ConsultarMalo(
        [FromQuery] string categoria, [FromServices] IWebHostEnvironment entorno, CancellationToken cancellationToken) =>
        entorno.IsDevelopment() ? Ok(await demo.ConsultarMaloAsync(categoria, cancellationToken)) : NotFound();

    /// <summary>LO BUENO: parámetros y clave de partición.</summary>
    [HttpGet("bueno")]
    public async Task<IActionResult> ConsultarBueno([FromQuery] string categoria, CancellationToken cancellationToken) =>
        Ok(await demo.ConsultarBuenoAsync(categoria, cancellationToken));

    /// <summary>Consulta que cruza particiones (sin clave de partición).</summary>
    [HttpGet("cruzada")]
    public async Task<IActionResult> ConsultarCruzada([FromQuery] string nombre, CancellationToken cancellationToken) =>
        Ok(await demo.ConsultarCruzadaAsync(nombre, cancellationToken));
}
