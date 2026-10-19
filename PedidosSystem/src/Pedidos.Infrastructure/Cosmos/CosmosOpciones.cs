using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Microsoft.Azure.Cosmos;

namespace Pedidos.Infrastructure.Cosmos;

/// <summary>Configuración compartida de Cosmos DB: nombres de la base y del cliente que usan el SDK y EF Core.</summary>
public static class CosmosOpciones
{
    public const string BaseDatos = "PedidosDb";
    public const string ContenedorCatalogo = "Productos";
    public const string ClaveParticionCatalogo = "/categoria";

    /// <summary>
    /// Crea el <see cref="CosmosClient"/>. Se registra como Singleton: está pensado para compartirse en toda la aplicación
    /// (crear uno por request agota las conexiones).
    /// </summary>
    public static CosmosClient CrearCliente(string cadenaConexion) =>
        new(cadenaConexion, new CosmosClientOptions
        {
            // El emulador solo soporta modo gateway y usa un certificado autofirmado
            ConnectionMode = ConnectionMode.Gateway,
            HttpClientFactory = CrearHttpClient,
            // camelCase: Id se guarda como "id" (lo que exige Cosmos) sin poner atributos de Cosmos en la entidad de Domain
            UseSystemTextJsonSerializerWithOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase },
            RequestTimeout = TimeSpan.FromSeconds(10)
        });

    public static HttpClient CrearHttpClient() =>
        new(new HttpClientHandler { ServerCertificateCustomValidationCallback = ValidarCertificado });

    // Solo se tolera el certificado autofirmado del emulador (localhost). Contra una cuenta real de Azure, la validación sigue activa.
    private static bool ValidarCertificado(
        HttpRequestMessage solicitud, X509Certificate2? certificado, System.Security.Cryptography.X509Certificates.X509Chain? cadena,
        System.Net.Security.SslPolicyErrors errores) =>
        errores == System.Net.Security.SslPolicyErrors.None || solicitud.RequestUri?.IsLoopback == true;
}
