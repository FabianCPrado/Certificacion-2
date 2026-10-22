using Asistente.Application.Interfaces;
using MediatR;

namespace Asistente.Application.Asistente;

public sealed record VerificarSaludQuery : IRequest<SaludDto>;

public sealed class VerificarSaludQueryHandler(IVerificadorSalud verificador)
    : IRequestHandler<VerificarSaludQuery, SaludDto>
{
    public Task<SaludDto> Handle(VerificarSaludQuery request, CancellationToken cancellationToken) =>
        verificador.VerificarAsync(cancellationToken);
}
