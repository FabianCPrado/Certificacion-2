using FluentValidation;
using MediatR;
using Pedidos.Application.Common;
using Pedidos.Application.Interfaces.Auditoria;

namespace Pedidos.Application.Auditoria.Queries.ListarAuditoria;

/// <summary>Un registro de auditoría. Solo transporta datos: no tiene lógica.</summary>
public sealed record AuditoriaDto(
    Guid Id,
    DateTime FechaUtc,
    string Metodo,
    string Ruta,
    int EstadoHttp,
    int DuracionMs,
    string Usuario,
    string TraceId,
    string? Request);

public sealed record ListarAuditoriaQuery(
    int Pagina = 1, int Tamanio = 20, string? Ruta = null, DateTime? Desde = null, DateTime? Hasta = null)
    : IQuery<PagedResult<AuditoriaDto>>;

public sealed class ListarAuditoriaQueryValidator : AbstractValidator<ListarAuditoriaQuery>
{
    public ListarAuditoriaQueryValidator()
    {
        RuleFor(q => q.Pagina).GreaterThanOrEqualTo(1);
        RuleFor(q => q.Tamanio).InclusiveBetween(1, 100);
        RuleFor(q => q.Hasta).GreaterThanOrEqualTo(q => q.Desde).When(q => q.Desde is not null && q.Hasta is not null);
    }
}

public sealed class ListarAuditoriaQueryHandler(IAuditoriaConsultas consultas)
    : IRequestHandler<ListarAuditoriaQuery, PagedResult<AuditoriaDto>>
{
    public Task<PagedResult<AuditoriaDto>> Handle(ListarAuditoriaQuery request, CancellationToken cancellationToken) =>
        consultas.ListarAsync(request.Pagina, request.Tamanio, request.Ruta, request.Desde, request.Hasta, cancellationToken);
}
