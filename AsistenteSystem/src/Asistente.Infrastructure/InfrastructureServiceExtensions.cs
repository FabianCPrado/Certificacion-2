using Anthropic;
using Asistente.Application.Interfaces;
using Asistente.Infrastructure.IA;
using Asistente.Infrastructure.Mapa;
using Asistente.Infrastructure.Servicios;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Asistente.Infrastructure;

public static class InfrastructureServiceExtensions
{
    private static readonly TimeSpan TiempoMaximo = TimeSpan.FromSeconds(10);

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration, string rutaMapa, string rutaSistemas, string carpetaConocimiento)
    {
        // Falla al iniciar si el mapa y las herramientas del código no coinciden
        var mapa = new MapaSistema(rutaMapa);
        Herramientas.Validar(mapa.Herramientas);
        services.AddSingleton<IMapaSistema>(mapa);

        // Base de conocimiento: estructura de los sistemas y documentos de arquitectura e instrucciones
        services.AddSingleton<IEstructuraSistemas>(new EstructuraSistemas(rutaSistemas));
        services.AddSingleton<IBibliotecaDocumentos>(new BibliotecaDocumentos(carpetaConocimiento));

        // Nada se conecta al arrancar: los HttpClient y el token se usan recién cuando alguien los necesita
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ITokenProvider, TokenProvider>();
        services.AddTransient<BearerTokenHandler>();

        AgregarCliente(services, TokenProvider.NombreCliente, configuration["Servicios:Authentication"]);
        AgregarCliente(services, VerificadorSalud.Catalogo, configuration["Servicios:Catalogo"]).AddHttpMessageHandler<BearerTokenHandler>();
        AgregarCliente(services, VerificadorSalud.Pedidos, configuration["Servicios:Pedidos"]);
        AgregarCliente(services, VerificadorSalud.Clientes, configuration["Servicios:Clientes"]);
        if (configuration["IA:Url"] is { Length: > 0 } urlOllama)
            AgregarCliente(services, VerificadorSalud.Modelo, urlOllama);

        services.AddSingleton<ICatalogoConsultas, CatalogoConsultasHttp>();
        services.AddSingleton<IPedidosConsultas, PedidosConsultasHttp>();
        services.AddSingleton<IClientesConsultas, ClientesConsultasHttp>();
        services.AddSingleton<Herramientas>();

        services.AddScoped<IVerificadorSalud, VerificadorSalud>();

        AgregarAsistente(services);
        return services;
    }

    // Con IA:Proveedor = Anthropic y una clave en IA:ApiKey (User Secrets) se usa Claude; si falta, el asistente
    // arranca igual y responde 503 "no configurado".
    // Se decide al resolver el servicio (no al registrarlo), con la configuración ya completa: así un cambio de
    // configuración posterior (p. ej. en los tests) también se respeta.
    private static void AgregarAsistente(IServiceCollection services)
    {
        // Las dos IAs (negocio y sistemas) comparten un único cliente de Claude
        services.AddSingleton(proveedores => CrearModelo(proveedores));
        services.AddSingleton<HerramientasSistemas>();

        services.AddSingleton<IAsistente>(proveedores =>
            proveedores.GetRequiredService<ModeloConfigurado>() is { Chat: { } chat } modelo
                ? new AsistenteIA(chat, proveedores.GetRequiredService<Herramientas>(), modelo.Nombre,
                    proveedores.GetRequiredService<ILogger<AsistenteIA>>())
                : new AsistenteNoConfigurado());

        services.AddSingleton<ISistemasAsistente>(proveedores =>
            proveedores.GetRequiredService<ModeloConfigurado>() is { Chat: { } chat } modelo
                ? new SistemasIA(chat, proveedores.GetRequiredService<HerramientasSistemas>(), modelo.Nombre,
                    proveedores.GetRequiredService<ILogger<SistemasIA>>())
                : new SistemasNoConfigurado());
    }

    /// <summary>El cliente de Claude y su nombre; <c>Chat</c> es null si falta IA:Proveedor = Anthropic o IA:ApiKey.</summary>
    private sealed record ModeloConfigurado(IChatClient? Chat, string Nombre);

    private static ModeloConfigurado CrearModelo(IServiceProvider proveedores)
    {
        var configuration = proveedores.GetRequiredService<IConfiguration>();
        var usaAnthropic = string.Equals(configuration["IA:Proveedor"], "Anthropic", StringComparison.OrdinalIgnoreCase);
        var apiKey = configuration["IA:ApiKey"];
        var nombre = configuration["IA:ModeloChat"] ?? "claude-haiku-5-5";

        if (!usaAnthropic || string.IsNullOrWhiteSpace(apiKey))
            return new ModeloConfigurado(null, nombre);

        var maxIteraciones = configuration.GetValue("IA:MaxIteraciones", 8);
        var chat = new AnthropicClient { ApiKey = apiKey }
            .AsIChatClient(nombre)
            .AsBuilder()
            .UseFunctionInvocation(proveedores.GetService<ILoggerFactory>(), f => f.MaximumIterationsPerRequest = maxIteraciones)
            .Build();

        return new ModeloConfigurado(chat, nombre);
    }

    private static IHttpClientBuilder AgregarCliente(IServiceCollection services, string nombre, string? url) =>
        services.AddHttpClient(nombre, cliente =>
        {
            cliente.BaseAddress = new Uri(url ?? throw new InvalidOperationException($"Falta la dirección de {nombre} en appsettings."));
            cliente.Timeout = TiempoMaximo;
        });
}
