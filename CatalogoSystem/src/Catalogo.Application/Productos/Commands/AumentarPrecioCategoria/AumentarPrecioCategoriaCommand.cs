using Catalogo.Application.Interfaces;
using FluentValidation;
using MediatR;

namespace Catalogo.Application.Productos.Commands.AumentarPrecioCategoria;

/// <summary>Sube el precio de toda una categoría un porcentaje. Devuelve cuántos productos cambió.</summary>
public record AumentarPrecioCategoriaCommand(string Categoria, decimal Porcentaje) : IRequest<int>;

public sealed class AumentarPrecioCategoriaValidator : AbstractValidator<AumentarPrecioCategoriaCommand>
{
    public AumentarPrecioCategoriaValidator()
    {
        RuleFor(c => c.Categoria).NotEmpty();
        RuleFor(c => c.Porcentaje).GreaterThan(0);
    }
}

public sealed class AumentarPrecioCategoriaCommandHandler(ICatalogoRepository catalogo) : IRequestHandler<AumentarPrecioCategoriaCommand, int>
{
    public Task<int> Handle(AumentarPrecioCategoriaCommand request, CancellationToken cancellationToken) =>
        catalogo.AumentarPrecioPorCategoriaAsync(request.Categoria, request.Porcentaje, cancellationToken);
}
