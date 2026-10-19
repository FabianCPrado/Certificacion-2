-- Datos de prueba para las demos de la Clase 4 (base PedidosDb).
-- Carga 100 clientes y 1.000 pedidos con 3 líneas cada uno. Los productos viven en el servicio Catálogo:
-- las líneas guardan un id y una categoría de ejemplo (no hace falta que existan en CatalogoDb para las demos de consultas).
-- Es idempotente: si ya hay clientes, no hace nada.
SET NOCOUNT ON;

IF EXISTS (SELECT 1 FROM Clientes)
BEGIN
    PRINT 'Ya hay datos de prueba: no se carga nada.';
    RETURN;
END;

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

-- 3 líneas por pedido, con productos de ejemplo distintos
INSERT INTO LineasPedido (Id, PedidoId, ProductoId, Categoria, NombreProducto, PrecioUnitario, Moneda, Cantidad)
SELECT NEWID(), p.PedidoId, CONVERT(NVARCHAR(36), NEWID()),
       CASE (p.i + k.k) % 4 WHEN 0 THEN 'Bebidas' WHEN 1 THEN 'Almacén' WHEN 2 THEN 'Limpieza' ELSE 'Lácteos' END,
       CONCAT('Producto ', (p.i * 37 + k.k * 15013) % 50000),
       (((p.i * 37 + k.k * 15013) % 500) * 100) + 99, 'COP', 1 + ((p.i + k.k) % 5)
FROM #ped p
CROSS JOIN (VALUES (0), (1), (2)) AS k(k);

DROP TABLE #cli, #ped;

PRINT 'Datos de prueba cargados.';
