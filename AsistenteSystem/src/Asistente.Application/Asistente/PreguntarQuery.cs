using Asistente.Application.Interfaces;
using FluentValidation;
using MediatR;

namespace Asistente.Application.Asistente;

public sealed record PreguntarQuery(string Pregunta, Guid? ClienteId) : IRequest<RespuestaDto>;

public sealed class PreguntarQueryValidator : AbstractValidator<PreguntarQuery>
{
    public PreguntarQueryValidator()
    {
        RuleFor(x => x.Pregunta).NotEmpty().Length(3, 500);
        RuleFor(x => x.ClienteId).NotEqual(Guid.Empty).When(x => x.ClienteId is not null);
    }
}

public sealed class PreguntarQueryHandler(IAsistente asistente) : IRequestHandler<PreguntarQuery, RespuestaDto>
{
    public Task<RespuestaDto> Handle(PreguntarQuery request, CancellationToken cancellationToken) =>
        asistente.ResponderAsync(new PreguntaRequest(request.Pregunta, request.ClienteId), cancellationToken);
}
