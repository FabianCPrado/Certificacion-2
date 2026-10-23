namespace Asistente.Tests.Contratos;

/// <summary>Ubica los archivos reales del repositorio (sistemas.json, docs, launchSettings) desde el directorio de los tests.</summary>
internal static class RepositorioReal
{
    public static string? Raiz()
    {
        for (var carpeta = new DirectoryInfo(AppContext.BaseDirectory); carpeta is not null; carpeta = carpeta.Parent)
        {
            if (File.Exists(Path.Combine(carpeta.FullName, "AsistenteSystem.slnx")))
                return carpeta.Parent?.FullName;
        }

        return null;
    }

    public static string DeAsistente(string relativa) => Path.Combine(Raiz()!, "AsistenteSystem", relativa);
}
