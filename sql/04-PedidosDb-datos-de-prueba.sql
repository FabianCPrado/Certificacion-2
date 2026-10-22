-- Datos de prueba para las demos de la Clase 4 (base PedidosDb).
-- Carga 100 clientes y 1.000 pedidos con 3 líneas cada uno. Los productos viven en el servicio Catálogo:
-- cada línea toma id, categoría, nombre y precio de un producto real de CatalogoDb, así que se puede consultar su
-- precio y su stock. Cada cliente tiene 5 productos favoritos que se repiten en sus pedidos ("lo de siempre").
-- Requiere haber ejecutado antes 03-CatalogoDb-crear.sql (misma instancia).
-- Es idempotente: si ya hay clientes, no hace nada.
SET NOCOUNT ON;

IF EXISTS (SELECT 1 FROM Clientes)
BEGIN
    PRINT 'Ya hay datos de prueba: no se carga nada.';
    RETURN;
END;

IF DB_ID('CatalogoDb') IS NULL
    THROW 50001, 'No existe CatalogoDb: ejecuta antes 03-CatalogoDb-crear.sql.', 1;

-- Catálogo numerado, para repartir los productos entre los clientes
SELECT ROW_NUMBER() OVER (ORDER BY Categoria, Nombre, Id) AS rn, Id, Categoria, Nombre, Precio
INTO #cat FROM CatalogoDb.dbo.Productos;

DECLARE @n INT = (SELECT COUNT(*) FROM #cat);
IF @n < 13
    THROW 50002, 'CatalogoDb tiene muy pocos productos: ejecuta 03-CatalogoDb-crear.sql.', 1;

-- 100 clientes
;WITH n AS (
    SELECT TOP (100) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS i FROM sys.all_objects
)
INSERT INTO Clientes (Id, Nombre, Email)
SELECT NEWID(), CONCAT('Cliente ', i), CONCAT('cliente', i, '@mail.com') FROM n;

-- Tabla de trabajo numerada, para repartir los clientes sin ORDER BY NEWID()
SELECT ROW_NUMBER() OVER (ORDER BY Id) AS rn, Id, Email INTO #cli FROM Clientes;

-- 1.000 pedidos repartidos en los últimos 90 días (20% borrador, 9% cancelados, el resto confirmados)
SELECT i, NEWID() AS PedidoId
INTO #ped
FROM (SELECT TOP (1000) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS i
      FROM sys.all_objects a CROSS JOIN sys.all_objects b) n;

INSERT INTO Pedidos (Id, ClienteId, EmailCliente, Calle, Ciudad, CodigoPostal, Pais, Moneda, Estado, CreadoEn)
SELECT p.PedidoId, c.Id, c.Email,
       CONCAT('Calle ', p.i, ' # 10-20'), 'Bogotá', '110111', 'Colombia', 'COP',
       CASE WHEN p.i % 5 = 0 THEN 'Borrador' WHEN p.i % 11 = 0 THEN 'Cancelado' ELSE 'Confirmado' END,
       DATEADD(DAY, -(p.i % 90), SYSUTCDATETIME())
FROM #ped p
JOIN #cli c ON c.rn = (p.i % 100) + 1;

-- 3 líneas por pedido con productos reales del Catálogo. El cliente número c = (i % 100) + 1 tiene 5 favoritos:
-- favorito j = (c * 7 + j * 3) % N; la línea k del pedido i usa el favorito j = ((i / 100) + k) % 5.
INSERT INTO LineasPedido (Id, PedidoId, ProductoId, Categoria, NombreProducto, PrecioUnitario, Moneda, Cantidad)
SELECT NEWID(), p.PedidoId, cat.Id, cat.Categoria, cat.Nombre, cat.Precio, 'COP', 1 + ((p.i + k.k) % 5)
FROM #ped p
CROSS JOIN (VALUES (0), (1), (2)) AS k(k)
JOIN #cat cat ON cat.rn = ((((p.i % 100) + 1) * 7 + (((p.i / 100) + k.k) % 5) * 3) % @n) + 1;

DROP TABLE #cli, #ped, #cat;

PRINT 'Datos de prueba cargados.';
