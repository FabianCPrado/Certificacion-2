using Asistente.Application.Asistente;

namespace Asistente.Tests.Application.Asistente;

public class PreguntarQueryValidatorTests
{
    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    public void Pregunta_vacia_o_corta_no_es_valida(string pregunta) =>
        Assert.False(new PreguntarQueryValidator().Validate(new PreguntarQuery(pregunta, null)).IsValid);

    [Fact]
    public void Pregunta_de_mas_de_500_caracteres_no_es_valida() =>
        Assert.False(new PreguntarQueryValidator().Validate(new PreguntarQuery(new string('a', 501), null)).IsValid);

    [Fact]
    public void ClienteId_vacio_no_es_valido() =>
        Assert.False(new PreguntarQueryValidator().Validate(new PreguntarQuery("hay stock?", Guid.Empty)).IsValid);

    [Fact]
    public void Pregunta_valida_pasa() =>
        Assert.True(new PreguntarQueryValidator().Validate(new PreguntarQuery("hay stock de yerba?", Guid.NewGuid())).IsValid);
}
