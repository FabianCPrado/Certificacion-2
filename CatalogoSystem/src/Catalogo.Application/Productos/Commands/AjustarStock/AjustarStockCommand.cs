using Catalogo.Application.Exceptions;
using Catalogo.Application.Interfaces;
using Catalogo.Domain.Common;
using FluentValidation;
using MediatR;

namespace Catalogo.Application.Productos.Commands.AjustarStock;

/// <summary>
/// Descuenta (Cantidad &lt; 0 en el repositorio) o repone (Cantidad &gt; 0) stock de forma atómica.
/// Falla con 404 si el producto no existe y con 409 si no alcanza el stock.
/// </summary>
public record AjustarStockCommand(string Categoria, string Id, int Cantidad, bool Descontar) : IRequest;

public sealed class AjustarStockValidator : AbstractValidator<AjustarStockCommand>
{
    public AjustarStockValidator() => RuleFor(c => c.Cantidad).GreaterThan(0);
}

public sealed class AjustarStockCommandHandler(ICatalogoRepository catalogo) : IRequestHandler<AjustarStockCommand>
{
    public async Task Handle(AjustarStockCommand request, CancellationToken cancellationToken)
    {
        var delta = request.Descontar ? -request.Cantidad : request.Cantidad;
        if (await catalogo.AjustarStockAsync(request.Id, request.Categoria, delta, cancellationToken))
            return;

        // No se actualizó ninguna fila: o no existe, o no alcanza el stock
        if (await catalogo.ObtenerAsync(request.Id, request.Categoria, cancellationToken) is null)
            throw new NotFoundException($"No existe el producto {request.Id}.");

        throw new ReglaNegocioException($"No hay stock suficiente del producto {request.Id}.");
    }
}
