using Asistente.Application.Interfaces;
using Asistente.Domain;
using MediatR;

namespace Asistente.Application.Asistente;

public sealed record ListarHerramientasQuery : IRequest<IReadOnlyList<HerramientaInfo>>;

public sealed class ListarHerramientasQueryHandler(IMapaSistema mapa)
    : IRequestHandler<ListarHerramientasQuery, IReadOnlyList<HerramientaInfo>>
{
    public Task<IReadOnlyList<HerramientaInfo>> Handle(ListarHerramientasQuery request, CancellationToken cancellationToken) =>
        Task.FromResult(mapa.Herramientas);
}
