namespace Pedidos.Application.Productos.Queries.ObtenerProducto;

/// <summary>Datos de un producto del Catálogo. Solo transporta datos: no tiene lógica.</summary>
public sealed record ProductoDto(string Id, string Categoria, string Nombre, decimal Precio, int Stock);
