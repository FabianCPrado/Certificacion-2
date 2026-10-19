using FluentValidation;
using MediatR;
using Pedidos.Application.Exceptions;
using Pedidos.Application.Interfaces.Pedidos;
using Pedidos.Application.Pedidos.Commands.CrearPedido;
using Pedidos.Domain.ValueObjects;
using Pedidos.Application.Common;

namespace Pedidos.Application.Pedidos.Commands.ActualizarPedido;

/// <summary>Cambia el email y la dirección de envío de un pedido en borrador.</summary>
public sealed record ActualizarPedidoCommand(Guid PedidoId, string EmailCliente, DireccionRequest Direccion) : ICommand;

public sealed class ActualizarPedidoCommandValidator : AbstractValidator<ActualizarPedidoCommand>
{
    public ActualizarPedidoCommandValidator()
    {
        RuleFor(c => c.PedidoId).NotEmpty();
        RuleFor(c => c.EmailCliente).NotEmpty().EmailAddress().MaximumLength(256);

        RuleFor(c => c.Direccion).NotNull().ChildRules(direccion =>
        {
            direccion.RuleFor(d => d.Calle).NotEmpty().MaximumLength(200);
            direccion.RuleFor(d => d.Ciudad).NotEmpty().MaximumLength(100);
            direccion.RuleFor(d => d.CodigoPostal).MaximumLength(20);
            direccion.RuleFor(d => d.Pais).NotEmpty().MaximumLength(100);
        });
    }
}

public sealed class ActualizarPedidoCommandHandler(IPedidoRepository pedidos)
    : IRequestHandler<ActualizarPedidoCommand>
{
    public async Task Handle(ActualizarPedidoCommand request, CancellationToken cancellationToken)
    {
        var pedido = await pedidos.ObtenerPorIdAsync(request.PedidoId, cancellationToken)
            ?? throw new NotFoundException($"No existe el pedido {request.PedidoId}.");

        var direccion = new Direccion(
            request.Direccion.Calle, request.Direccion.Ciudad, request.Direccion.CodigoPostal, request.Direccion.Pais);

        await pedidos.ActualizarAsync(pedido, new Email(request.EmailCliente), direccion, cancellationToken);
    }
}
