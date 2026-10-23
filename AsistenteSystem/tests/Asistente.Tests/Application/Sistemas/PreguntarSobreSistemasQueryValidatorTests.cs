using Asistente.Application.Sistemas;

namespace Asistente.Tests.Application.Sistemas;

public class PreguntarSobreSistemasQueryValidatorTests
{
    [Theory]
    [InlineData("", false)]
    [InlineData("ab", false)]
    [InlineData("¿Qué sistemas hay?", true)]
    public void La_pregunta_sobre_sistemas_tiene_entre_3_y_500_caracteres(string pregunta, bool valida) =>
        Assert.Equal(valida, new PreguntarSobreSistemasQueryValidator().Validate(new PreguntarSobreSistemasQuery(pregunta)).IsValid);
}
