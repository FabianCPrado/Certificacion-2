using Pedidos.Domain.Pedidos;
using Pedidos.Domain.ValueObjects;

namespace Pedidos.Application.Interfaces.Pedidos;

/// <summary>
/// Repositorio por agregado: la interfaz la define Application (lo que necesita)
/// y la implementación, con las reglas de negocio, vive en Infrastructure.
/// </summary>
public sealed record LineaNueva(Guid ProductoId, string NombreProducto, Dinero PrecioUnitario, int Cantidad);

public interface IPedidoRepository
{
    Task<Pedido?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Valida y crea un pedido en borrador, dejándolo pendiente de guardar.</summary>
    Pedido Crear(Guid clienteId, Email emailCliente, Direccion direccionEnvio, string moneda, DateTime creadoEn);

    /// <summary>Crea el pedido con todas sus líneas y lo persiste en una sola operación.</summary>
    Task<Pedido> CrearAsync(
        Guid clienteId, Email emailCliente, Direccion direccionEnvio, string moneda, DateTime creadoEn,
        IReadOnlyList<LineaNueva> lineas, CancellationToken cancellationToken = default);

    /// <summary>
    /// Crea el pedido con sus líneas y descuenta el stock de cada producto en una sola transacción:
    /// si falla cualquier paso (producto inexistente, stock insuficiente), se revierten los cambios.
    /// </summary>
    Task<Pedido> CrearYDescontarStockAsync(
        Guid clienteId, Email emailCliente, Direccion direccionEnvio, string moneda, DateTime creadoEn,
        IReadOnlyList<LineaNueva> lineas, CancellationToken cancellationToken = default);

    void AgregarLinea(Pedido pedido, Guid productoId, string nombreProducto, Dinero precioUnitario, int cantidad);

    void Confirmar(Pedido pedido);

    void Cancelar(Pedido pedido);

    /// <summary>Confirma el pedido (validando las reglas) y persiste el cambio.</summary>
    Task ConfirmarAsync(Pedido pedido, CancellationToken cancellationToken = default);

    /// <summary>Cancela el pedido (validando las reglas) y persiste el cambio.</summary>
    Task CancelarAsync(Pedido pedido, CancellationToken cancellationToken = default);

    /// <summary>Cambia el email y la dirección de envío (solo en borrador) y persiste el cambio.</summary>
    Task ActualizarAsync(Pedido pedido, Email emailCliente, Direccion direccionEnvio, CancellationToken cancellationToken = default);

    /// <summary>Elimina el pedido con sus líneas (solo en borrador).</summary>
    Task EliminarAsync(Pedido pedido, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancela, con un procedimiento almacenado, los pedidos en borrador con más de <paramref name="dias"/> días
    /// sin confirmarse. Devuelve cuántos canceló.
    /// </summary>
    Task<int> CancelarBorradoresAntiguosAsync(int dias, CancellationToken cancellationToken = default);
}
