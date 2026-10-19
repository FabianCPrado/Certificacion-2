-- Procedimientos almacenados de la Clase 4 (base PedidosDb). Se pueden ejecutar varias veces (CREATE OR ALTER).

-- Descuenta stock de un producto. Lo llama ProductoRepository con EF Core (ExecuteSqlAsync).
CREATE OR ALTER PROCEDURE ActualizarStock
    @ProductoId UNIQUEIDENTIFIER,
    @Cantidad INT
AS
    UPDATE Productos SET Stock = Stock - @Cantidad WHERE Id = @ProductoId;
GO

-- Cancela los pedidos en borrador con más de @Dias días sin confirmarse y devuelve cuántos canceló
-- en el parámetro de salida @Cancelados. Lo llama PedidoRepository con Dapper (ExecuteAsync).
CREATE OR ALTER PROCEDURE CancelarBorradoresAntiguos
    @Dias INT,
    @Cancelados INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE Pedidos
    SET Estado = 'Cancelado'
    WHERE Estado = 'Borrador' AND CreadoEn < DATEADD(DAY, -@Dias, SYSUTCDATETIME());

    SET @Cancelados = @@ROWCOUNT;
END;
GO

-- Ventas confirmadas por día y moneda. Lo llama PedidosConsultas con Dapper (QueryAsync).
CREATE OR ALTER PROCEDURE ObtenerVentasPorDia
    @Desde DATETIME2,
    @Hasta DATETIME2
AS
BEGIN
    SET NOCOUNT ON;

    SELECT CAST(p.CreadoEn AS date)           AS Dia,
           p.Moneda                           AS Moneda,
           SUM(l.Cantidad * l.PrecioUnitario) AS Total
    FROM Pedidos p
    JOIN LineasPedido l ON l.PedidoId = p.Id
    WHERE p.Estado = 'Confirmado' AND p.CreadoEn >= @Desde AND p.CreadoEn < @Hasta
    GROUP BY CAST(p.CreadoEn AS date), p.Moneda
    ORDER BY Dia, Moneda;
END;
GO
