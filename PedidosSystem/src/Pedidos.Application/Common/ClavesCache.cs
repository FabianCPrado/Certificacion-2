namespace Pedidos.Application.Common;

/// <summary>Claves de Redis en un solo lugar: quien guarda y quien invalida usan exactamente la misma.</summary>
public static class ClavesCache
{
    public static string Producto(Guid id) => $"pedidos:v1:producto:{id}";

    public static string PedidosPorFechas(DateOnly desde, DateOnly hasta) =>
        $"pedidos:v1:pedidos-por-fechas:{desde:yyyyMMdd}:{hasta:yyyyMMdd}";
}
