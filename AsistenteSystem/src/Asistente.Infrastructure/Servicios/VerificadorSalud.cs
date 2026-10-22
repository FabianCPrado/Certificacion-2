using System.Diagnostics;
using System.Text.Json;
using Anthropic;
using Anthropic.Exceptions;
using Asistente.Application.Asistente;
using Asistente.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Asistente.Infrastructure.Servicios;

/// <summary>
/// Llama a un endpoint de lectura de cada servicio. Como cada uno consulta su propia base, una respuesta correcta
/// confirma la API y su base. El asistente no entra directo a ninguna base (regla 1).
/// </summary>
public sealed class VerificadorSalud(
    IHttpClientFactory httpClientFactory, ITokenProvider tokens, IMapaSistema mapa, IConfiguration configuration)
    : IVerificadorSalud
{
    public const string Catalogo = "Catalogo";
    public const string Pedidos = "Pedidos";
    public const string Clientes = "Clientes";
    public const string Modelo = "Modelo";

    private static readonly TimeSpan Limite = TimeSpan.FromSeconds(5);

    public async Task<SaludDto> VerificarAsync(CancellationToken cancellationToken = default)
    {
        var autenticacion = Medir("Authentication", "API + AuthenticationDB", TokenAsync, cancellationToken);

        var componentes = await Task.WhenAll(
            Medir("Mapa", "mapa-sistema.json", () => Task.FromResult($"{mapa.Herramientas.Count} herramientas cargadas"), cancellationToken),
            autenticacion,
            CatalogoAsync(autenticacion, cancellationToken),
            Medir("Pedidos", "API + PedidosDb", () => ContarAsync(Pedidos, "/api/pedidos?tamanio=1", "pedidos", cancellationToken), cancellationToken),
            Medir("Clientes", "API + ClientesDb", () => ContarAsync(Clientes, "/api/clientes?tamanio=1", "clientes", cancellationToken), cancellationToken),
            MedirModelo(cancellationToken));

        var sano = componentes.All(c => c.Estado != "Error");
        return new SaludDto(sano ? "Sano" : "Degradado", componentes);
    }

    // Catálogo exige JWT: si Authentication no lo entrega, se dice así en vez de culpar a Catálogo de no responder
    private async Task<ComponenteSalud> CatalogoAsync(Task<ComponenteSalud> autenticacion, CancellationToken cancellationToken)
    {
        const string conecta = "API + CatalogoDb";

        if ((await autenticacion).Estado == "Error")
            return new ComponenteSalud(Catalogo, conecta, "Error", "Sin JWT: Authentication no lo entrega, no se puede consultar Catálogo", 0);

        return await Medir(Catalogo, conecta,
            () => ContarAsync(Catalogo, "/api/productos/categorias", "categorías", cancellationToken), cancellationToken);
    }

    private async Task<string> TokenAsync()
    {
        var token = await tokens.ObtenerAsync(renovar: true);
        return $"JWT emitido para asistente-service, vence {token.ExpiraEn:HH:mm:ss} UTC";
    }

    // Catálogo requiere token; Pedidos y Clientes hoy no (el handler Bearer solo está en Catálogo)
    private async Task<string> ContarAsync(string cliente, string ruta, string unidad, CancellationToken cancellationToken)
    {
        var http = httpClientFactory.CreateClient(cliente);
        using var respuesta = await http.GetAsync(ruta, cancellationToken);

        if (respuesta.StatusCode == System.Net.HttpStatusCode.Forbidden)
            throw new HttpRequestException("HTTP 403: asistente-service todavía no tiene permiso de lectura en este servicio.");
        respuesta.EnsureSuccessStatusCode();

        using var json = await JsonDocument.ParseAsync(await respuesta.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        var cantidad = json.RootElement.ValueKind == JsonValueKind.Array
            ? json.RootElement.GetArrayLength()
            : json.RootElement.GetProperty("total").GetInt32();
        return $"responde, {cantidad} {unidad}";
    }

    private Task<ComponenteSalud> MedirModelo(CancellationToken cancellationToken)
    {
        if (string.Equals(configuration["IA:Proveedor"], "Anthropic", StringComparison.OrdinalIgnoreCase))
        {
            var modelo = configuration["IA:ModeloChat"];
            var apiKey = configuration["IA:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
                return Task.FromResult(new ComponenteSalud("Modelo", $"Claude ({modelo})", "SinConfigurar", "Falta IA:ApiKey (User Secrets)", 0));

            // Listar modelos valida la clave sin gastar tokens
            return Medir("Modelo", $"Claude ({modelo})", async () =>
            {
                try
                {
                    await new AnthropicClient { ApiKey = apiKey }.Models.List(new(), cancellationToken);
                }
                catch (AnthropicException ex)
                {
                    throw new InvalidOperationException($"Anthropic rechazó la consulta: {ex.Message}", ex);
                }

                return "Claude responde y la clave es válida";
            }, cancellationToken);
        }

        if (!string.Equals(configuration["IA:Proveedor"], "Ollama", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(new ComponenteSalud("Modelo", "IA", "SinConfigurar", "El proveedor de IA todavía no está conectado", 0));

        return Medir("Modelo", $"Ollama ({configuration["IA:ModeloChat"]})", async () =>
        {
            var http = httpClientFactory.CreateClient(Modelo);
            using var respuesta = await http.GetAsync("/api/tags", cancellationToken);
            respuesta.EnsureSuccessStatusCode();
            return "Ollama responde";
        }, cancellationToken);
    }

    private static async Task<ComponenteSalud> Medir(
        string nombre, string conecta, Func<Task<string>> comprobar, CancellationToken cancellationToken)
    {
        var reloj = Stopwatch.StartNew();
        try
        {
            using var limite = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            limite.CancelAfter(Limite);
            var detalle = await comprobar().WaitAsync(limite.Token);
            return new ComponenteSalud(nombre, conecta, "Ok", detalle, reloj.ElapsedMilliseconds);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new ComponenteSalud(nombre, conecta, "Error", $"No respondió en {Limite.TotalSeconds:0} s", reloj.ElapsedMilliseconds);
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or JsonException or KeyNotFoundException)
        {
            return new ComponenteSalud(nombre, conecta, "Error", ex.InnerException?.Message ?? ex.Message, reloj.ElapsedMilliseconds);
        }
    }
}
