using Clientes.Domain.Common;
using Clientes.Domain.ValueObjects;

namespace Clientes.Domain.Tests;

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
