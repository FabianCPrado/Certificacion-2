using Catalogo.Application.Interfaces;
using MediatR;

namespace Catalogo.Application.Productos.Commands.EliminarProducto;

/// <summary>Devuelve false si el producto no existe.</summary>
public record EliminarProductoCommand(string Categoria, string Id) : IRequest<bool>;

public sealed class EliminarProductoCommandHandler(ICatalogoRepository catalogo) : IRequestHandler<EliminarProductoCommand, bool>
{
    public Task<bool> Handle(EliminarProductoCommand request, CancellationToken cancellationToken) =>
        catalogo.EliminarAsync(request.Id, request.Categoria, cancellationToken);
}
