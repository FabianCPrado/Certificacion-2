using System.Text.Json;

namespace Clientes.Application.Interfaces.Pedidos;

/// <summary>
/// Lo que Clientes necesita saber del servicio Pedidos. La define Application y la implementa Infrastructure
/// con una llamada REST: esta base ya no ve la tabla de pedidos.
/// </summary>
public interface IPedidosConsultas
{
    /// <summary>¿El cliente tiene al menos un pedido? Si Pedidos no responde, lanza <see cref="HttpRequestException"/>.</summary>
    Task<bool> ClienteTienePedidosAsync(Guid clienteId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pide a Pedidos los pedidos de un rango de fechas. Pedidos decide si salen de su Redis o de su SQL;
    /// Clientes solo recibe la respuesta. Si Pedidos no responde, lanza <see cref="HttpRequestException"/>.
    /// </summary>
    Task<PedidosPorFechasDto> ListarPorFechasAsync(DateOnly desde, DateOnly hasta, CancellationToken cancellationToken = default);
}

/// <summary>La respuesta de Pedidos tal cual: los pedidos se dejan como JSON porque Clientes no los modela.</summary>
public sealed record PedidosPorFechasDto(JsonElement Pedidos, int Cantidad, string Origen, double Milisegundos);
