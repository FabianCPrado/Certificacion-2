namespace Pedidos.Application.Common;

/// <summary>Página de resultados de un listado. Solo transporta datos.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Pagina, int Tamanio, int Total);
