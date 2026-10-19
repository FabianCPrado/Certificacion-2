namespace Pedidos.Infrastructure.Persistence;

/// <summary>
/// Cadena de conexión a SQL Server para el código que no usa EF Core (Dapper). Se registra una vez en
/// <see cref="InfrastructureServiceExtensions"/>, así esas clases no dependen del DbContext solo para conectarse.
/// </summary>
public sealed record ConexionSql(string CadenaConexion);
