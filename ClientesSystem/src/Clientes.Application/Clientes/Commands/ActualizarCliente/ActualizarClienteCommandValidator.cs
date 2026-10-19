using FluentValidation;

namespace Clientes.Application.Clientes.Commands.ActualizarCliente;

public sealed class ActualizarClienteCommandValidator : AbstractValidator<ActualizarClienteCommand>
{
    public ActualizarClienteCommandValidator()
    {
        RuleFor(c => c.ClienteId).NotEmpty();
        RuleFor(c => c.Nombre).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Email).NotEmpty().EmailAddress().MaximumLength(256);
    }
}
