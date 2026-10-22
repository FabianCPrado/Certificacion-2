using System.ComponentModel;
using Asistente.Application.Exceptions;
using Asistente.Application.Interfaces;
using Asistente.Domain;
using Microsoft.Extensions.AI;

namespace Asistente.Infrastructure.IA;

/// <summary>Qué herramientas usó el asistente en una pregunta, de dónde salieron los datos y si un servicio falló.</summary>
public sealed class RegistroUso
{
    private readonly List<string> _herramientas = [];
    private readonly List<Fuente> _fuentes = [];

    /// <summary>Si la pregunta se corta cuando un servicio falla, en vez de dejar que el modelo siga hablando.</summary>
    public CancellationTokenSource? Corte { get; set; }

    public ServicioNoDisponibleException? Falla { get; private set; }

    public IReadOnlyList<string> HerramientasUsadas() => _herramientas.Distinct().ToList();

    public IReadOnlyList<Fuente> FuentesUsadas() => _fuentes.Distinct().ToList();

    internal void Usar(string herramienta) => _herramientas.Add(herramienta);

    internal void Origen(string servicio, string referencia) => _fuentes.Add(new Fuente(servicio, referencia));

    internal void Fallar(ServicioNoDisponibleException falla)
    {
        Falla = falla;
        Corte?.Cancel();
    }
}

/// <summary>
/// Las herramientas que Claude puede llamar. Nombres y descripciones salen de mapa-sistema.json y solo se registran
/// las de solo lectura. Si el código y el mapa no coinciden, el servicio no arranca (<see cref="Validar"/>).
/// </summary>
public sealed class Herramientas(
    IMapaSistema mapa, ICatalogoConsultas catalogo, IPedidosConsultas pedidos, IClientesConsultas clientes)
{
    public const string ObtenerPrecioYStock = "ObtenerPrecioYStock";
    public const string BuscarProductos = "BuscarProductos";
    public const string UltimosPedidosDeCliente = "UltimosPedidosDeCliente";
    public const string EstadoDePedido = "EstadoDePedido";
    public const string DatosDeCliente = "DatosDeCliente";

    private static readonly string[] Implementadas =
        [ObtenerPrecioYStock, BuscarProductos, UltimosPedidosDeCliente, EstadoDePedido, DatosDeCliente];

    /// <summary>Falla si una herramienta del código no está en el mapa, o al revés.</summary>
    public static void Validar(IReadOnlyList<HerramientaInfo> delMapa)
    {
        var nombres = delMapa.Select(h => h.Nombre).ToHashSet();
        var sinMapa = Implementadas.Where(n => !nombres.Contains(n)).ToList();
        var sinCodigo = nombres.Where(n => !Implementadas.Contains(n)).ToList();

        if (sinMapa.Count > 0 || sinCodigo.Count > 0)
            throw new InvalidOperationException(
                $"El mapa y el código no coinciden. Faltan en el mapa: [{string.Join(", ", sinMapa)}]. " +
                $"Faltan en el código: [{string.Join(", ", sinCodigo)}].");
    }

    /// <summary>Las herramientas de solo lectura del mapa, ligadas al registro de esta pregunta.</summary>
    public IReadOnlyList<AITool> Crear(RegistroUso registro)
    {
        var porNombre = new Dictionary<string, Delegate>
        {
            [ObtenerPrecioYStock] = (
                [Description("Categoría del producto, p. ej. Abarrotes")] string categoria,
                [Description("Id del producto")] string id,
                CancellationToken ct) =>
                Ejecutar(registro, ObtenerPrecioYStock, async () =>
                {
                    var producto = await catalogo.ObtenerAsync(categoria, id, ct);
                    if (producto is null)
                        return "No existe ese producto.";
                    registro.Origen("Catalogo", $"{producto.Categoria}/{producto.Id}");
                    return producto;
                }),
            [BuscarProductos] = (
                [Description("Parte del nombre del producto, una palabra clave corta, p. ej. yerba")] string nombre,
                [Description("Opcional: Abarrotes, Bebidas, Conservas, Galletas, Lacteos o Limpieza. Si se omite busca en todas")] string? categoria,
                CancellationToken ct) =>
                Ejecutar(registro, BuscarProductos, async () =>
                {
                    var encontrados = await catalogo.BuscarAsync(categoria, nombre, ct);
                    foreach (var p in encontrados)
                        registro.Origen("Catalogo", $"{p.Categoria}/{p.Id}");
                    return encontrados.Count == 0 ? "No se encontraron productos." : encontrados;
                }),
            [UltimosPedidosDeCliente] = (
                [Description("Id (guid) del cliente")] Guid clienteId,
                [Description("Cuántos pedidos traer, de 1 a 20")] int cantidad,
                CancellationToken ct) =>
                Ejecutar(registro, UltimosPedidosDeCliente, async () =>
                {
                    var lista = await pedidos.UltimosDeClienteAsync(clienteId, cantidad, ct);
                    foreach (var p in lista)
                        registro.Origen("Pedidos", p.Id.ToString());
                    return lista.Count == 0 ? "El cliente no tiene pedidos." : lista;
                }),
            [EstadoDePedido] = (
                [Description("Id (guid) del pedido")] Guid pedidoId,
                CancellationToken ct) =>
                Ejecutar(registro, EstadoDePedido, async () =>
                {
                    var pedido = await pedidos.ObtenerAsync(pedidoId, ct);
                    if (pedido is null)
                        return "No existe ese pedido.";
                    registro.Origen("Pedidos", pedido.Id.ToString());
                    return pedido;
                }),
            [DatosDeCliente] = (
                [Description("Id (guid) del cliente")] Guid clienteId,
                CancellationToken ct) =>
                Ejecutar(registro, DatosDeCliente, async () =>
                {
                    var cliente = await clientes.ObtenerAsync(clienteId, ct);
                    if (cliente is null)
                        return "No existe ese cliente.";
                    registro.Origen("Clientes", cliente.Id.ToString());
                    return cliente;
                })
        };

        return mapa.Herramientas
            .Where(h => h.SoloLectura)
            .Select(h => (AITool)AIFunctionFactory.Create(porNombre[h.Nombre], h.Nombre, h.Descripcion))
            .ToList();
    }

    // Anota el uso y, si un servicio no responde, corta la pregunta: el asistente devuelve 503 en vez de
    // dejar que el modelo invente una explicación.
    private static async Task<object> Ejecutar(RegistroUso registro, string herramienta, Func<Task<object>> consulta)
    {
        registro.Usar(herramienta);
        try
        {
            return await consulta();
        }
        catch (ServicioNoDisponibleException ex)
        {
            registro.Fallar(ex);
            throw;
        }
    }
}
