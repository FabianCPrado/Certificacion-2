-- Crea la base ClientesDb en la misma instancia que PedidosDb. Es idempotente.
-- Si PedidosDb existe y ya tiene clientes, los copia (mismos Id: Pedidos.ClienteId sigue siendo válido).
-- Uso (LocalDB): sqlcmd -S "(localdb)\MSSQLLocalDB" -E -i scripts\crear-base-de-datos.sql
SET NOCOUNT ON;

IF DB_ID('ClientesDb') IS NULL
    CREATE DATABASE ClientesDb;
GO

USE ClientesDb;
GO

IF OBJECT_ID('dbo.Clientes', 'U') IS NULL
CREATE TABLE dbo.Clientes (
    Id      UNIQUEIDENTIFIER NOT NULL,
    Nombre  NVARCHAR(200)    NOT NULL,
    Email   NVARCHAR(256)    NOT NULL,
    CONSTRAINT PK_Clientes PRIMARY KEY (Id)
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Clientes_Email' AND object_id = OBJECT_ID('dbo.Clientes'))
    CREATE UNIQUE INDEX IX_Clientes_Email ON dbo.Clientes (Email);
GO

-- Copia los clientes existentes de PedidosDb (si hay) sin duplicar Id ni Email
IF DB_ID('PedidosDb') IS NOT NULL
    EXEC('
        INSERT INTO dbo.Clientes (Id, Nombre, Email)
        SELECT o.Id, ISNULL(o.Nombre, N''(sin nombre)''), o.Email
        FROM PedidosDb.dbo.Clientes o
        WHERE o.Email IS NOT NULL
          AND NOT EXISTS (SELECT 1 FROM dbo.Clientes c WHERE c.Id = o.Id OR c.Email = o.Email);
    ');
GO

PRINT 'Base ClientesDb lista.';
GO
