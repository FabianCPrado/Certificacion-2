using Asistente.Application.Interfaces;
using FluentValidation;
using MediatR;

namespace Asistente.Application.Asistente;

public sealed record ProponerPedidoQuery(Guid ClienteId, string Pedido) : IRequest<PropuestaPedidoDto>;

public sealed class ProponerPedidoQueryValidator : AbstractValidator<ProponerPedidoQuery>
{
    public ProponerPedidoQueryValidator()
    {
        RuleFor(x => x.ClienteId).NotEqual(Guid.Empty);
        RuleFor(x => x.Pedido).NotEmpty().Length(3, 200);
    }
}

public sealed class ProponerPedidoQueryHandler(IAsistente asistente) : IRequestHandler<ProponerPedidoQuery, PropuestaPedidoDto>
{
    public Task<PropuestaPedidoDto> Handle(ProponerPedidoQuery request, CancellationToken cancellationToken) =>
        asistente.ProponerPedidoAsync(new PropuestaRequest(request.ClienteId, request.Pedido), cancellationToken);
}
