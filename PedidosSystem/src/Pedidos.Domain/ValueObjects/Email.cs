using Pedidos.Domain.Common;

namespace Pedidos.Domain.ValueObjects;

public sealed record Email
{
    public Email(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new ReglaNegocioException("El email es obligatorio.");

        var normalizado = valor.Trim().ToLowerInvariant();
        var arroba = normalizado.IndexOf('@');

        var formatoValido = arroba > 0
            && arroba == normalizado.LastIndexOf('@')
            && normalizado[(arroba + 1)..].Contains('.');

        if (!formatoValido)
            throw new ReglaNegocioException("El email no tiene un formato válido.");

        Valor = normalizado;
    }

    public string Valor { get; }

    public override string ToString() => Valor;
}
