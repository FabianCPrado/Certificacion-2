using Catalogo.Application.Interfaces;
using FluentValidation;
using MediatR;

namespace Catalogo.Application.Productos.Commands.ActualizarProducto;

/// <summary>Devuelve false si el producto no existe. La categoría identifica al producto y no se cambia.</summary>
public record ActualizarProductoCommand(string Categoria, string Id, string Nombre, decimal Precio, int Stock) : IRequest<bool>;

public sealed class ActualizarProductoValidator : AbstractValidator<ActualizarProductoCommand>
{
    public ActualizarProductoValidator()
    {
        RuleFor(c => c.Nombre).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Precio).GreaterThan(0);
        RuleFor(c => c.Stock).GreaterThanOrEqualTo(0);
    }
}

public sealed class ActualizarProductoCommandHandler(ICatalogoRepository catalogo) : IRequestHandler<ActualizarProductoCommand, bool>
{
    public async Task<bool> Handle(ActualizarProductoCommand request, CancellationToken cancellationToken)
    {
        var producto = await catalogo.ObtenerAsync(request.Id, request.Categoria, cancellationToken);
        if (producto is null)
            return false;

        producto.Nombre = request.Nombre;
        producto.Precio = request.Precio;
        producto.Stock = request.Stock;
        await catalogo.GuardarAsync(producto, cancellationToken);
        return true;
    }
}
