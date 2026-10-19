namespace Pedidos.Application.Productos.Queries.ObtenerProducto;

/// <summary>Datos de un producto que devuelve la API. Solo transporta datos: no tiene lógica.</summary>
public sealed record ProductoDto(Guid Id, string Nombre, string Categoria, decimal Precio, int Stock);
