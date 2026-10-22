namespace Asistente.Domain;

// La estructura de los sistemas y la base de conocimiento (arquitectura e instrucciones).
// Se leen de sistemas.json y de los archivos .md; este proyecto solo define sus formas.

public sealed record EndpointInfo(string Metodo, string Ruta, string Descripcion, string Acceso);

public sealed record CapaInfo(string Proyecto, string Responsabilidad);

/// <summary>Un sistema (o servicio externo) que este sistema llama, para qué y cómo.</summary>
public sealed record DependenciaInfo(string Sistema, string Para, string Como);

/// <summary>Lo que un sistema expone y otro consume: cambiarlo sin avisar rompe al consumidor.</summary>
public sealed record ContratoInfo(string Nombre, string Descripcion, IReadOnlyList<string> UsadoPor);

public sealed record SistemaInfo(
    string Nombre,
    string Carpeta,
    string Descripcion,
    int PuertoHttps,
    int PuertoHttp,
    string? BaseDeDatos,
    IReadOnlyList<string> Tecnologias,
    IReadOnlyList<CapaInfo> Capas,
    IReadOnlyList<EndpointInfo> Endpoints,
    IReadOnlyList<DependenciaInfo> Consume,
    IReadOnlyList<ContratoInfo> Contratos,
    IReadOnlyList<string> Tests,
    IReadOnlyList<string> Notas);

/// <summary>Un documento de la base de conocimiento. <c>Tipo</c>: "arquitectura" o "instrucciones".</summary>
public sealed record DocumentoInfo(string Id, string Tipo, string Titulo, string Resumen, string Contenido);
