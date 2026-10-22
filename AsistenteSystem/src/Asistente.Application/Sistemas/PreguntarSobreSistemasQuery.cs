using Asistente.Application.Asistente;
using Asistente.Application.Interfaces;
using FluentValidation;
using MediatR;

namespace Asistente.Application.Sistemas;

public sealed record PreguntaSistemasRequest(string Pregunta);

public sealed record PreguntarSobreSistemasQuery(string Pregunta) : IRequest<RespuestaDto>;

public sealed class PreguntarSobreSistemasQueryValidator : AbstractValidator<PreguntarSobreSistemasQuery>
{
    public PreguntarSobreSistemasQueryValidator() => RuleFor(q => q.Pregunta).NotEmpty().Length(3, 500);
}

public sealed class PreguntarSobreSistemasQueryHandler(ISistemasAsistente asistente)
    : IRequestHandler<PreguntarSobreSistemasQuery, RespuestaDto>
{
    public Task<RespuestaDto> Handle(PreguntarSobreSistemasQuery request, CancellationToken cancellationToken) =>
        asistente.PreguntarAsync(request.Pregunta, cancellationToken);
}
