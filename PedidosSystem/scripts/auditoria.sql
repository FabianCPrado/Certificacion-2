-- Tabla de auditoría de las operaciones POST, PUT y DELETE (base PedidosDb). Se puede ejecutar varias veces.
IF OBJECT_ID(N'dbo.Auditoria', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Auditoria
    (
        Id         UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Auditoria PRIMARY KEY,
        FechaUtc   DATETIME2        NOT NULL,
        Metodo     NVARCHAR(10)     NOT NULL,
        Ruta       NVARCHAR(300)    NOT NULL,
        EstadoHttp INT              NOT NULL,
        DuracionMs INT              NOT NULL,
        Usuario    NVARCHAR(100)    NOT NULL,
        TraceId    NVARCHAR(100)    NOT NULL
    );

    CREATE INDEX IX_Auditoria_FechaUtc ON dbo.Auditoria (FechaUtc);
    CREATE INDEX IX_Auditoria_Ruta     ON dbo.Auditoria (Ruta);
END;
GO

-- Cuerpo del request (JSON tal cual llega, recortado a 4.000 caracteres). Nulo si la operación no tiene cuerpo.
IF COL_LENGTH(N'dbo.Auditoria', N'Request') IS NULL
    ALTER TABLE dbo.Auditoria ADD Request NVARCHAR(MAX) NULL;
GO
