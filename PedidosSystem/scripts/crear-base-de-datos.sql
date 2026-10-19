-- Crea la base PedidosDb completa: tablas, claves e índices. Los productos NO están aquí: viven en CatalogoSystem (CatalogoDb).
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
    ProductoId      NVARCHAR(100)    NOT NULL,   -- id del producto en el servicio Catálogo
    Categoria       NVARCHAR(100)    NOT NULL,   -- categoría del producto en el Catálogo (con el id lo identifica)
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

-- Bases creadas antes de mover los productos al Catálogo: las líneas pasan a guardar id string + categoría
-- y se elimina la tabla local de productos. Es idempotente.
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.LineasPedido') AND name = 'ProductoId'
           AND system_type_id = TYPE_ID('uniqueidentifier'))
    ALTER TABLE dbo.LineasPedido ALTER COLUMN ProductoId NVARCHAR(100) NOT NULL;
GO

IF COL_LENGTH('dbo.LineasPedido', 'Categoria') IS NULL
    ALTER TABLE dbo.LineasPedido ADD Categoria NVARCHAR(100) NOT NULL CONSTRAINT DF_LineasPedido_Categoria DEFAULT '';
GO

DROP PROCEDURE IF EXISTS dbo.ActualizarStock;
DROP TABLE IF EXISTS dbo.Productos;
GO

PRINT 'Base PedidosDb lista.';
GO
