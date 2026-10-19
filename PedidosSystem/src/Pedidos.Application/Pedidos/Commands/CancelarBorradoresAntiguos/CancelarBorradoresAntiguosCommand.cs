using FluentValidation;
using MediatR;
using Pedidos.Application.Common;
using Pedidos.Application.Interfaces.Pedidos;

namespace Pedidos.Application.Pedidos.Commands.CancelarBorradoresAntiguos;

/// <summary>Cancela los pedidos en borrador con más de <paramref name="Dias"/> días sin confirmarse. Devuelve cuántos canceló.</summary>
public sealed record CancelarBorradoresAntiguosCommand(int Dias) : ICommand<int>;

public sealed class CancelarBorradoresAntiguosCommandValidator : AbstractValidator<CancelarBorradoresAntiguosCommand>
{
    public CancelarBorradoresAntiguosCommandValidator() =>
        RuleFor(c => c.Dias).InclusiveBetween(1, 365);
}

public sealed class CancelarBorradoresAntiguosCommandHandler(IPedidoRepository pedidos)
    : IRequestHandler<CancelarBorradoresAntiguosCommand, int>
{
    public Task<int> Handle(CancelarBorradoresAntiguosCommand request, CancellationToken cancellationToken) =>
        pedidos.CancelarBorradoresAntiguosAsync(request.Dias, cancellationToken);
}
