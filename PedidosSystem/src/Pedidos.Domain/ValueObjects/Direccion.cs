using Pedidos.Domain.Common;

namespace Pedidos.Domain.ValueObjects;

public sealed record Direccion
{
    public Direccion(string calle, string ciudad, string codigoPostal, string pais)
    {
        if (string.IsNullOrWhiteSpace(calle) || string.IsNullOrWhiteSpace(ciudad) || string.IsNullOrWhiteSpace(pais))
            throw new ReglaNegocioException("La calle, la ciudad y el país son obligatorios.");

        Calle = calle.Trim();
        Ciudad = ciudad.Trim();
        CodigoPostal = codigoPostal?.Trim() ?? string.Empty;
        Pais = pais.Trim();
    }

    public string Calle { get; }

    public string Ciudad { get; }

    public string CodigoPostal { get; }

    public string Pais { get; }
}
