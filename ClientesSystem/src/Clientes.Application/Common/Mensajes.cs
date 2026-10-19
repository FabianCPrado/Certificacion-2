using MediatR;

namespace Clientes.Application.Common;

// CQRS: cada mensaje es un comando (cambia estado) o una consulta (solo lee), nunca los dos.
// - Un comando pasa por su IXRepository (reglas de negocio + persistencia).
// - Una consulta pasa por su IXConsultas (proyección al DTO, sin tracking).
// Los tests de arquitectura verifican que cada lado use solo lo suyo.

/// <summary>Comando sin respuesta: cambia el estado del sistema.</summary>
public interface ICommand : IRequest;

/// <summary>Comando con una respuesta mínima (por ejemplo, el Id de lo creado).</summary>
public interface ICommand<out TResponse> : IRequest<TResponse>;

/// <summary>Consulta: devuelve datos y no cambia el estado del sistema.</summary>
public interface IQuery<out TResponse> : IRequest<TResponse>;
