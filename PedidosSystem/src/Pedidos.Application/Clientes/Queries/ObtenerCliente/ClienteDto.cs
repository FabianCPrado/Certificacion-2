namespace Pedidos.Application.Clientes.Queries.ObtenerCliente;

/// <summary>Datos de un cliente que devuelve la API. Solo transporta datos: no tiene lógica.</summary>
public sealed record ClienteDto(Guid Id, string Nombre, string Email);
