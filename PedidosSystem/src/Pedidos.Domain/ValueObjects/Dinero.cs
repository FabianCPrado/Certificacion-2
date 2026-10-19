using Pedidos.Domain.Common;

namespace Pedidos.Domain.ValueObjects;

/// <summary>Objeto de valor: monto + moneda, inmutable y comparado por valor.</summary>
public sealed record Dinero
{
    public Dinero(decimal monto, string moneda)
    {
        if (monto < 0)
            throw new ReglaNegocioException("El monto no puede ser negativo.");

        if (!EsMonedaValida(moneda))
            throw new ReglaNegocioException("La moneda debe ser un código ISO de 3 letras en mayúsculas.");

        Monto = decimal.Round(monto, 2);
        Moneda = moneda;
    }

    public decimal Monto { get; }

    public string Moneda { get; }

    public static Dinero Cero(string moneda) => new(0m, moneda);

    public static bool EsMonedaValida(string? moneda) =>
        moneda is { Length: 3 } && moneda.All(char.IsAsciiLetterUpper);

    public Dinero Sumar(Dinero otro)
    {
        if (otro.Moneda != Moneda)
            throw new ReglaNegocioException($"No se pueden sumar {Moneda} y {otro.Moneda}.");

        return new Dinero(Monto + otro.Monto, Moneda);
    }

    public Dinero Multiplicar(int factor) => new(Monto * factor, Moneda);

    public override string ToString() => $"{Monto:0.00} {Moneda}";
}
