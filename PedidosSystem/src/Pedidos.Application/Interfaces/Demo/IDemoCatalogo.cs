using Pedidos.Domain.Catalogo;

namespace Pedidos.Application.Interfaces.Demo;

/// <summary>Un producto leído con el costo de la operación en RU (Request Units).</summary>
public sealed record LecturaCatalogoDto(double Ru, ProductoCatalogo Producto);

/// <summary>Resultado de una consulta del catálogo: cuántos productos devolvió y cuántas RU costó.</summary>
public sealed record ConsultaCatalogoDto(int Cantidad, double Ru);

/// <summary>
/// Demo de la Clase 5 (Cosmos DB con el SDK): carga de datos, lectura puntual y consultas que muestran el costo en RU.
/// La implementación con <c>CosmosClient</c> vive en Infrastructure.
/// </summary>
public interface IDemoCatalogo
{
    /// <summary>Carga 300 productos con atributos distintos por categoría (se puede repetir: reemplaza los mismos ids).</summary>
    Task<int> CargarAsync(CancellationToken cancellationToken = default);

    /// <summary>Point read: id + clave de partición. Nulo si no existe.</summary>
    Task<LecturaCatalogoDto?> LeerPuntualAsync(string categoria, string id, CancellationToken cancellationToken = default);

    /// <summary>MALO, A PROPÓSITO: arma la consulta concatenando el texto (demo de inyección). Solo para la clase.</summary>
    Task<ConsultaCatalogoDto> ConsultarMaloAsync(string categoria, CancellationToken cancellationToken = default);

    /// <summary>BUENO: consulta con parámetros y dentro de la partición de la categoría.</summary>
    Task<ConsultaCatalogoDto> ConsultarBuenoAsync(string categoria, CancellationToken cancellationToken = default);

    /// <summary>Consulta sin clave de partición: recorre todas las particiones y cuesta más.</summary>
    Task<ConsultaCatalogoDto> ConsultarCruzadaAsync(string nombre, CancellationToken cancellationToken = default);
}

/// <summary>Demo de la Clase 5 (proveedor de EF Core para Cosmos DB): el mismo catálogo con DbContext y LINQ.</summary>
public interface IDemoCatalogoEf
{
    /// <summary>Guarda 30 productos con un solo SaveChanges.</summary>
    Task<int> CargarAsync(CancellationToken cancellationToken = default);

    /// <summary>Productos de una categoría con LINQ, dentro de la partición.</summary>
    Task<IReadOnlyList<ProductoCatalogo>> ListarAsync(string categoria, CancellationToken cancellationToken = default);
}
