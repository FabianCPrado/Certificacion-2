-- Crea la base AuthenticationDB desde cero, con la tabla de clientes de servicio (los que piden JWT en POST /api/tokens).
-- Es la misma estructura que crea Authentication con EF Core: registra la migración InitialAuthenticationSchema en
-- __EFMigrationsHistory, así que al arrancar la API (MigrateAsync) no intenta crear nada otra vez.
-- Es idempotente.
--
-- Los clientes de servicio NO se cargan aquí: su secreto se guarda hasheado (ASP.NET Core Identity, PBKDF2) y ese hash
-- no se puede calcular en T-SQL. Los crea la API al arrancar en Development, a partir de User Secrets:
--   ClientCredentials:PedidosSecret    -> cliente "pedidos-service"
--   ClientCredentials:AsistenteSecret  -> cliente "asistente-service"
-- (cada secreto de 32 a 256 caracteres; ver AuthenticationSystem/README.md). La clave de firma del token
-- (Jwt:SigningKey) tampoco va en la base: también es un User Secret.
-- Uso (LocalDB): sqlcmd -S "(localdb)\MSSQLLocalDB" -E -f 65001 -i 06-AuthenticationDb-crear.sql
SET NOCOUNT ON;

IF DB_ID('AuthenticationDB') IS NULL
    CREATE DATABASE AuthenticationDB;
GO

USE AuthenticationDB;
GO

IF OBJECT_ID('dbo.ServiceClients', 'U') IS NULL
CREATE TABLE dbo.ServiceClients (
    ClientId   NVARCHAR(100)     NOT NULL,
    Name       NVARCHAR(200)     NOT NULL,
    SecretHash NVARCHAR(1000)    NOT NULL,
    IsActive   BIT               NOT NULL,
    CreatedAt  DATETIMEOFFSET    NOT NULL,
    CONSTRAINT PK_ServiceClients PRIMARY KEY (ClientId)
);
GO

-- Historial de migraciones de EF Core: marca el esquema como ya aplicado
IF OBJECT_ID('dbo.__EFMigrationsHistory', 'U') IS NULL
CREATE TABLE dbo.__EFMigrationsHistory (
    MigrationId    NVARCHAR(150) NOT NULL,
    ProductVersion NVARCHAR(32)  NOT NULL,
    CONSTRAINT PK___EFMigrationsHistory PRIMARY KEY (MigrationId)
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = '20261020003604_InitialAuthenticationSchema')
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261020003604_InitialAuthenticationSchema', '10.0.12');
GO

PRINT 'Base AuthenticationDB lista. Los clientes de servicio los crea la API al arrancar (User Secrets).';
GO
