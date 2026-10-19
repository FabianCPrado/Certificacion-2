-- Crea la base PedidosDb completa: tablas, claves, índices y procedimiento almacenado.
-- Equivale al modelo de EF Core (carpeta Persistence/MapeoEntidades). Es idempotente: se puede ejecutar varias veces.
-- Para cargar datos de prueba, ejecutar después scripts/seed-clase4.sql.
-- Uso (LocalDB): sqlcmd -S "(localdb)\MSSQLLocalDB" -E -i scripts\crear-base-de-datos.sql
SET NOCOUNT ON;

IF DB_ID('PedidosDb') IS NULL
    CREATE DATABASE PedidosDb;
GO

USE PedidosDb;
GO

-- Clientes
IF OBJECT_ID('dbo.Clientes', 'U') IS NULL
CREATE TABLE dbo.Clientes (
    Id      UNIQUEIDENTIFIER NOT NULL,
    Nombre  NVARCHAR(200)    NULL,
    Email   NVARCHAR(256)    NULL,
    CONSTRAINT PK_Clientes PRIMARY KEY (Id)
);
GO

-- Productos
IF OBJECT_ID('dbo.Productos', 'U') IS NULL
CREATE TABLE dbo.Productos (
    Id         UNIQUEIDENTIFIER NOT NULL,
    Nombre     NVARCHAR(200)    NULL,
    Categoria  NVARCHAR(50)     NULL,
    Precio     DECIMAL(18, 2)   NOT NULL,
    Stock      INT              NOT NULL,
    CONSTRAINT PK_Productos PRIMARY KEY (Id)
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Productos_Categoria' AND object_id = OBJECT_ID('dbo.Productos'))
    CREATE INDEX IX_Productos_Categoria ON dbo.Productos (Categoria);
GO

-- Pedidos (EmailCliente y la dirección de envío son objetos de valor guardados en la misma tabla)
IF OBJECT_ID('dbo.Pedidos', 'U') IS NULL
CREATE TABLE dbo.Pedidos (
    Id            UNIQUEIDENTIFIER NOT NULL,
    ClienteId     UNIQUEIDENTIFIER NOT NULL,
    EmailCliente  NVARCHAR(256)    NULL,
    Calle         NVARCHAR(200)    NULL,
    Ciudad        NVARCHAR(100)    NULL,
    CodigoPostal  NVARCHAR(20)     NULL,
    Pais          NVARCHAR(100)    NULL,
    Moneda        NCHAR(3)         NOT NULL,
    Estado        NVARCHAR(20)     NOT NULL,   -- Borrador | Confirmado | Cancelado
    CreadoEn      DATETIME2        NOT NULL,
    CONSTRAINT PK_Pedidos PRIMARY KEY (Id),
    CONSTRAINT CK_Pedidos_Estado CHECK (Estado IN ('Borrador', 'Confirmado', 'Cancelado'))
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Pedidos_ClienteId' AND object_id = OBJECT_ID('dbo.Pedidos'))
    CREATE INDEX IX_Pedidos_ClienteId ON dbo.Pedidos (ClienteId);
GO

-- Líneas de pedido (PrecioUnitario y Moneda vienen del objeto de valor Dinero)
IF OBJECT_ID('dbo.LineasPedido', 'U') IS NULL
CREATE TABLE dbo.LineasPedido (
    Id              UNIQUEIDENTIFIER NOT NULL,
    PedidoId        UNIQUEIDENTIFIER NOT NULL,
    ProductoId      UNIQUEIDENTIFIER NOT NULL,
    NombreProducto  NVARCHAR(200)    NULL,
    PrecioUnitario  DECIMAL(18, 2)   NOT NULL,
    Moneda          NCHAR(3)         NOT NULL,
    Cantidad        INT              NOT NULL,
    CONSTRAINT PK_LineasPedido PRIMARY KEY (Id),
    CONSTRAINT FK_LineasPedido_Pedidos_PedidoId FOREIGN KEY (PedidoId)
        REFERENCES dbo.Pedidos (Id) ON DELETE CASCADE
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LineasPedido_PedidoId' AND object_id = OBJECT_ID('dbo.LineasPedido'))
    CREATE INDEX IX_LineasPedido_PedidoId ON dbo.LineasPedido (PedidoId);
GO

-- Auditoría de operaciones que modifican datos (POST, PUT, DELETE)
IF OBJECT_ID('dbo.Auditoria', 'U') IS NULL
CREATE TABLE dbo.Auditoria (
    Id          UNIQUEIDENTIFIER NOT NULL,
    FechaUtc    DATETIME2        NOT NULL,
    Metodo      NVARCHAR(10)     NULL,
    Ruta        NVARCHAR(300)    NULL,
    EstadoHttp  INT              NOT NULL,
    DuracionMs  INT              NOT NULL,
    Usuario     NVARCHAR(100)    NULL,
    TraceId     NVARCHAR(100)    NULL,
    Request     NVARCHAR(MAX)    NULL,
    CONSTRAINT PK_Auditoria PRIMARY KEY (Id)
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Auditoria_FechaUtc' AND object_id = OBJECT_ID('dbo.Auditoria'))
    CREATE INDEX IX_Auditoria_FechaUtc ON dbo.Auditoria (FechaUtc);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Auditoria_Ruta' AND object_id = OBJECT_ID('dbo.Auditoria'))
    CREATE INDEX IX_Auditoria_Ruta ON dbo.Auditoria (Ruta);
GO

-- Descuenta stock de un producto (la usan PedidoRepository y ProductoRepository).
-- La condición Stock >= @Cantidad evita que el stock quede negativo si dos pedidos compiten.
CREATE OR ALTER PROCEDURE dbo.ActualizarStock
    @ProductoId UNIQUEIDENTIFIER,
    @Cantidad   INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Productos
    SET Stock = Stock - @Cantidad
    WHERE Id = @ProductoId
      AND Stock >= @Cantidad;
END;
GO

PRINT 'Base PedidosDb lista.';
GO
