using System.ComponentModel;
using Asistente.Application.Exceptions;
using Asistente.Application.Sistemas;
using Microsoft.Extensions.AI;

namespace Asistente.Infrastructure.IA;

/// <summary>
/// Las herramientas con las que la IA de sistemas consulta el ecosistema: la estructura de cada sistema, el impacto de
/// un cambio y los documentos de arquitectura e instrucciones. Si algo no existe, se lo cuentan al modelo (con los
/// nombres válidos) en vez de fallar, para que pueda corregir la consulta.
/// </summary>
public sealed class HerramientasSistemas(ConocimientoEcosistema conocimiento)
{
    public const string ListarSistemas = "ListarSistemas";
    public const string ObtenerSistema = "ObtenerSistema";
    public const string ImpactoDeSistema = "ImpactoDeSistema";
    public const string BuscarEnConocimiento = "BuscarEnConocimiento";
    public const string ListarDocumentos = "ListarDocumentos";
    public const string LeerDocumento = "LeerDocumento";

    public IReadOnlyList<AITool> Crear(RegistroUso registro) =>
    [
        AIFunctionFactory.Create(
            () => Ejecutar(registro, ListarSistemas, () => conocimiento.Listar()),
            ListarSistemas,
            "Lista todos los sistemas del ecosistema con su descripción, puertos, base de datos y de quién dependen. Úsala para dar una vista general."),
        AIFunctionFactory.Create(
            ([Description("Nombre del sistema: Authentication, Catalogo, Pedidos, Clientes o Asistente")] string nombre) =>
                Ejecutar(registro, ObtenerSistema, () =>
                {
                    var sistema = conocimiento.Obtener(nombre);
                    registro.Origen("Sistemas", sistema.Nombre);
                    return sistema;
                }),
            ObtenerSistema,
            "Devuelve la estructura completa de un sistema: capas, endpoints, dependencias, contratos, tests y notas."),
        AIFunctionFactory.Create(
            ([Description("Nombre del sistema que se piensa cambiar")] string nombre) =>
                Ejecutar(registro, ImpactoDeSistema, () =>
                {
                    var impacto = conocimiento.Impacto(nombre);
                    registro.Origen("Sistemas", impacto.Sistema);
                    return impacto;
                }),
            ImpactoDeSistema,
            "Dice qué sistemas consumen a este y qué contratos usan: qué se puede romper si se lo cambia."),
        AIFunctionFactory.Create(
            ([Description("Texto a buscar, una o dos palabras, p. ej. stock, JWT, detalle")] string texto) =>
                Ejecutar(registro, BuscarEnConocimiento, () => texto.Trim().Length < 2
                    ? "Escribí al menos 2 caracteres para buscar."
                    : conocimiento.Buscar(texto)),
            BuscarEnConocimiento,
            "Busca un texto en la estructura de los sistemas, la arquitectura y las instrucciones. Úsala para ubicar dónde está algo."),
        AIFunctionFactory.Create(
            ([Description("arquitectura o instrucciones; vacío para ambos")] string? tipo) =>
                Ejecutar(registro, ListarDocumentos, () => conocimiento.Documentos(tipo)),
            ListarDocumentos,
            "Lista los documentos de arquitectura (cómo está pensado el sistema) y las instrucciones (guías paso a paso)."),
        AIFunctionFactory.Create(
            ([Description("arquitectura o instrucciones")] string tipo, [Description("Id del documento, p. ej. capas-y-dependencias")] string id) =>
                Ejecutar(registro, LeerDocumento, () =>
                {
                    var documento = conocimiento.Documento(tipo, id);
                    registro.Origen(documento.Tipo == "arquitectura" ? "Arquitectura" : "Instrucciones", documento.Id);
                    return documento;
                }),
            LeerDocumento,
            "Devuelve un documento completo de arquitectura o una guía paso a paso."),
    ];

    private static object Ejecutar(RegistroUso registro, string herramienta, Func<object> consulta)
    {
        registro.Usar(herramienta);
        try
        {
            return consulta();
        }
        catch (NotFoundException ex)
        {
            return ex.Message;
        }
    }
}
