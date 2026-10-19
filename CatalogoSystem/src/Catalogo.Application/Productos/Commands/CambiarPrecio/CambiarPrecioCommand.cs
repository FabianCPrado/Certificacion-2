using Catalogo.Application.Interfaces;
using FluentValidation;
using MediatR;

namespace Catalogo.Application.Productos.Commands.CambiarPrecio;

/// <summary>Devuelve false si el producto no existe.</summary>
public record CambiarPrecioCommand(string Categoria, string Id, decimal Precio) : IRequest<bool>;

public sealed class CambiarPrecioValidator : AbstractValidator<CambiarPrecioCommand>
{
    public CambiarPrecioValidator() => RuleFor(c => c.Precio).GreaterThan(0);
}

public sealed class CambiarPrecioCommandHandler(ICatalogoRepository catalogo) : IRequestHandler<CambiarPrecioCommand, bool>
{
    public async Task<bool> Handle(CambiarPrecioCommand request, CancellationToken cancellationToken)
    {
        var producto = await catalogo.ObtenerAsync(request.Id, request.Categoria, cancellationToken);
        if (producto is null)
            return false;

        producto.Precio = request.Precio;
        await catalogo.GuardarAsync(producto, cancellationToken);
        return true;
    }
}
