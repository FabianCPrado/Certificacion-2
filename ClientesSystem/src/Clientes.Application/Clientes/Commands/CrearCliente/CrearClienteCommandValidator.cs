using FluentValidation;

namespace Clientes.Application.Clientes.Commands.CrearCliente;

/// <summary>Validación de forma (datos de entrada). Las reglas de negocio viven en el repositorio.</summary>
public sealed class CrearClienteCommandValidator : AbstractValidator<CrearClienteCommand>
{
    public CrearClienteCommandValidator()
    {
        RuleFor(c => c.Nombre).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Email).NotEmpty().EmailAddress().MaximumLength(256);
    }
}
