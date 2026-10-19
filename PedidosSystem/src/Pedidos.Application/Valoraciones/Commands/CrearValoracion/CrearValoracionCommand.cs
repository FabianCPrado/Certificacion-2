using FluentValidation;
using MediatR;
using Pedidos.Application.Common;
using Pedidos.Application.Interfaces.Valoraciones;

namespace Pedidos.Application.Valoraciones.Commands.CrearValoracion;

public sealed record CrearValoracionCommand(Guid ProductoId, string Usuario, int Puntaje, string? Comentario)
    : ICommand<Guid>;

public sealed class CrearValoracionCommandValidator : AbstractValidator<CrearValoracionCommand>
{
    public CrearValoracionCommandValidator()
    {
        RuleFor(c => c.ProductoId).NotEmpty();
        RuleFor(c => c.Usuario).NotEmpty().MaximumLength(100);
        RuleFor(c => c.Puntaje).InclusiveBetween(1, 5);
        RuleFor(c => c.Comentario).MaximumLength(500);
    }
}

/// <summary>Comando: solo usa <see cref="IValoracionRepository"/> (lado de escritura), nunca las consultas.</summary>
public sealed class CrearValoracionCommandHandler(IValoracionRepository repositorio)
    : IRequestHandler<CrearValoracionCommand, Guid>
{
    public Task<Guid> Handle(CrearValoracionCommand request, CancellationToken cancellationToken) =>
        repositorio.CrearAsync(
            new NuevaValoracion(request.ProductoId, request.Usuario, request.Puntaje, request.Comentario),
            cancellationToken);
}
