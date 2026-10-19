using Pedidos.Domain.Common;
using Pedidos.Domain.ValueObjects;

namespace Pedidos.Domain.Tests;

public class DineroTests
{
    [Fact]
    public void Dos_montos_iguales_son_iguales_por_valor()
    {
        Assert.Equal(new Dinero(10, "COP"), new Dinero(10, "COP"));
    }

    [Fact]
    public void Un_monto_negativo_lanza_excepcion()
    {
        Assert.Throws<ReglaNegocioException>(() => new Dinero(-1, "COP"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("co")]
    [InlineData("cop")]
    [InlineData("PESOS")]
    public void Una_moneda_invalida_lanza_excepcion(string moneda)
    {
        Assert.Throws<ReglaNegocioException>(() => new Dinero(10, moneda));
    }

    [Fact]
    public void Sumar_monedas_distintas_lanza_excepcion()
    {
        var pesos = new Dinero(10, "COP");
        var dolares = new Dinero(10, "USD");

        Assert.Throws<ReglaNegocioException>(() => pesos.Sumar(dolares));
    }
}

public class EmailTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("sin-arroba.com")]
    [InlineData("@ejemplo.com")]
    [InlineData("ana@@ejemplo.com")]
    [InlineData("ana@sin-punto")]
    public void Un_formato_invalido_lanza_excepcion(string valor)
    {
        Assert.Throws<ReglaNegocioException>(() => new Email(valor));
    }

    [Fact]
    public void Se_normaliza_a_minusculas_sin_espacios()
    {
        var email = new Email("  Ana@Ejemplo.COM ");

        Assert.Equal("ana@ejemplo.com", email.Valor);
    }
}
