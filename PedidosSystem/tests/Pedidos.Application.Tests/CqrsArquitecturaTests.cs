using MediatR;
using Pedidos.Application.Common;

namespace Pedidos.Application.Tests;

/// <summary>
/// Reglas de CQRS verificadas sobre el ensamblado de Application:
/// los comandos escriben (repositorios), las consultas leen (consultas) y nunca se cruzan.
/// </summary>
public class CqrsArquitecturaTests
{
    private static readonly System.Reflection.Assembly Application = typeof(ICommand).Assembly;

    /// <summary>Cada handler junto con el tipo de mensaje que atiende.</summary>
    private static List<(Type Handler, Type Mensaje)> Handlers() =>
        Application.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType
                    && (i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)
                        || i.GetGenericTypeDefinition() == typeof(IRequestHandler<>)))
                .Select(i => (Handler: t, Mensaje: i.GetGenericArguments()[0])))
            .ToList();

    private static IEnumerable<string> Dependencias(Type handler) =>
        handler.GetConstructors().SelectMany(c => c.GetParameters()).Select(p => p.ParameterType.Name);

    [Fact]
    public void Hay_handlers_que_verificar() => Assert.NotEmpty(Handlers());

    [Fact]
    public void Todo_mensaje_es_un_comando_o_una_consulta()
    {
        var sueltos = Handlers()
            .Where(h => !typeof(ICommand).IsAssignableFrom(h.Mensaje)
                && !h.Mensaje.GetInterfaces().Any(i => i.IsGenericType
                    && (i.GetGenericTypeDefinition() == typeof(ICommand<>) || i.GetGenericTypeDefinition() == typeof(IQuery<>))))
            .Select(h => h.Mensaje.Name);

        Assert.Empty(sueltos);
    }

    [Fact]
    public void Las_consultas_no_usan_repositorios_de_escritura()
    {
        var infractores = Handlers()
            .Where(h => h.Mensaje.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQuery<>)))
            .Where(h => Dependencias(h.Handler).Any(d => d.EndsWith("Repository", StringComparison.Ordinal)))
            .Select(h => h.Handler.Name);

        Assert.Empty(infractores);
    }

    [Fact]
    public void Los_comandos_no_usan_consultas()
    {
        var infractores = Handlers()
            .Where(h => typeof(ICommand).IsAssignableFrom(h.Mensaje)
                || h.Mensaje.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>)))
            .Where(h => Dependencias(h.Handler).Any(d => d.EndsWith("Consultas", StringComparison.Ordinal)))
            .Select(h => h.Handler.Name);

        Assert.Empty(infractores);
    }
}
