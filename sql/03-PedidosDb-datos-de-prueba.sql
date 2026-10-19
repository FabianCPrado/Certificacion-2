-- Datos de prueba para las demos de la Clase 4 (base PedidosDb, esquema de las migraciones).
-- Carga 50.000 productos, 100 clientes y 1.000 pedidos con 3 líneas cada uno.
-- Es idempotente: si ya hay productos, no hace nada.
SET NOCOUNT ON;

IF EXISTS (SELECT 1 FROM Productos)
BEGIN
    PRINT 'Ya hay datos de prueba: no se carga nada.';
    RETURN;
END;

-- 50.000 productos
;WITH n AS (
    SELECT TOP (50000) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS i
    FROM sys.all_objects a CROSS JOIN sys.all_objects b
)
INSERT INTO Productos (Id, Nombre, Categoria, Precio, Stock)
SELECT NEWID(),
       CONCAT('Producto ', i),
       CASE i % 4 WHEN 0 THEN 'Bebidas' WHEN 1 THEN 'Almacén' WHEN 2 THEN 'Limpieza' ELSE 'Lácteos' END,
       (i % 500) * 100 + 99,
       100
FROM n;

-- 100 clientes
;WITH n AS (
    SELECT TOP (100) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS i FROM sys.all_objects
)
INSERT INTO Clientes (Id, Nombre, Email)
SELECT NEWID(), CONCAT('Cliente ', i), CONCAT('cliente', i, '@mail.com') FROM n;

-- Tablas de trabajo numeradas, para repartir productos y clientes sin ORDER BY NEWID()
SELECT ROW_NUMBER() OVER (ORDER BY Id) AS rn, Id, Nombre, Precio INTO #prod FROM Productos;
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

-- 3 líneas por pedido, con productos distintos
INSERT INTO LineasPedido (Id, PedidoId, ProductoId, NombreProducto, PrecioUnitario, Moneda, Cantidad)
SELECT NEWID(), p.PedidoId, pr.Id, pr.Nombre, pr.Precio, 'COP', 1 + ((p.i + k.k) % 5)
FROM #ped p
CROSS JOIN (VALUES (0), (1), (2)) AS k(k)
JOIN #prod pr ON pr.rn = ((p.i * 37 + k.k * 15013) % 50000) + 1;

DROP TABLE #prod, #cli, #ped;

PRINT 'Datos de prueba cargados.';
