-- Crea la base CatalogoDb desde cero, con sus tablas y 41 productos de ejemplo en 6 categorías.
-- Es la misma estructura que crea CatalogoSystem con EF Core (EnsureCreated), así que la API la usa tal cual.
-- Los productos incluyen algunos sin stock (2) y con stock bajo (3), para probar los casos de "no hay stock".
-- Los ids son texto en minúsculas y estables: se calculan a partir de la categoría y el nombre (siempre dan el mismo id).
-- Es idempotente: no duplica tablas ni productos.
-- Uso (LocalDB): sqlcmd -S "(localdb)\MSSQLLocalDB" -E -f 65001 -i 03-CatalogoDb-crear.sql
SET NOCOUNT ON;

IF DB_ID('CatalogoDb') IS NULL
    CREATE DATABASE CatalogoDb;
GO

USE CatalogoDb;
GO

IF OBJECT_ID('dbo.Productos', 'U') IS NULL
CREATE TABLE dbo.Productos (
    Id         NVARCHAR(36)   NOT NULL,
    Categoria  NVARCHAR(100)  NOT NULL,
    Nombre     NVARCHAR(200)  NOT NULL,
    Precio     DECIMAL(18, 2) NOT NULL,
    Stock      INT            NOT NULL,
    CONSTRAINT PK_Productos PRIMARY KEY (Id)
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Productos_Categoria_Nombre' AND object_id = OBJECT_ID('dbo.Productos'))
    CREATE INDEX IX_Productos_Categoria_Nombre ON dbo.Productos (Categoria, Nombre);
GO

IF OBJECT_ID('dbo.ProductoAtributos', 'U') IS NULL
CREATE TABLE dbo.ProductoAtributos (
    ProductoId NVARCHAR(36)  NOT NULL,
    Parametro  NVARCHAR(200) NOT NULL,
    Valor      NVARCHAR(MAX) NOT NULL,
    CONSTRAINT PK_ProductoAtributos PRIMARY KEY (ProductoId, Parametro),
    CONSTRAINT FK_ProductoAtributos_Productos FOREIGN KEY (ProductoId)
        REFERENCES dbo.Productos (Id) ON DELETE CASCADE
);
GO

-- Productos de ejemplo (Presentacion se guarda como atributo)
DECLARE @p TABLE (Categoria NVARCHAR(100), Nombre NVARCHAR(200), Precio DECIMAL(18, 2), Stock INT, Presentacion NVARCHAR(100));

INSERT INTO @p (Categoria, Nombre, Precio, Stock, Presentacion) VALUES
-- Abarrotes
('Abarrotes', 'Aceite vegetal 900 ml',       3.89, 25, '900 ml'),
('Abarrotes', 'Arroz grano largo 1 kg',      2.49, 45, '1 kg'),
('Abarrotes', 'Azucar blanca 1 kg',          1.75, 35, '1 kg'),
('Abarrotes', 'Fideos spaghetti 500 g',      1.29, 60, '500 g'),
('Abarrotes', 'Yerba mate 1 kg',             4.20, 12, '1 kg'),
('Abarrotes', 'Cafe molido 500 g',           5.90,  0, '500 g'),
('Abarrotes', 'Te negro 25 sobres',          1.95, 40, '25 sobres'),
('Abarrotes', 'Harina de trigo 1 kg',        1.10, 55, '1 kg'),
('Abarrotes', 'Sal fina 500 g',              0.65, 70, '500 g'),
('Abarrotes', 'Cereal de maiz 500 g',        3.40, 22, '500 g'),
('Abarrotes', 'Avena en hojuelas 400 g',     2.05, 30, '400 g'),
('Abarrotes', 'Mermelada de durazno 400 g',  2.70, 16, '400 g'),
('Abarrotes', 'Azucar en sobres x50',        1.60,  2, '50 sobres'),
-- Bebidas
('Bebidas', 'Agua mineral 1.5 L',            1.10, 50, '1.5 L'),
('Bebidas', 'Jugo de naranja 1 L',           2.35, 24, '1 L'),
('Bebidas', 'Agua con gas 1.5 L',            1.25, 38, '1.5 L'),
('Bebidas', 'Jugo de manzana 1 L',           2.30, 20, '1 L'),
('Bebidas', 'Gaseosa cola 2 L',              2.60, 48, '2 L'),
('Bebidas', 'Gaseosa lima limon 2 L',        2.60,  0, '2 L'),
('Bebidas', 'Cerveza lager 473 ml',          1.90, 60, '473 ml'),
-- Conservas
('Conservas', 'Atun en aceite 170 g',        2.10, 30, '170 g'),
('Conservas', 'Sardinas en aceite 125 g',    1.75, 44, '125 g'),
('Conservas', 'Duraznos en almibar 820 g',   3.30, 14, '820 g'),
('Conservas', 'Tomate triturado 520 g',      1.45, 52, '520 g'),
('Conservas', 'Choclo en lata 300 g',        1.50, 26, '300 g'),
-- Galletas
('Galletas', 'Galletas de vainilla 300 g',   1.80, 32, '300 g'),
('Galletas', 'Galletas de chocolate 300 g',  2.00, 28, '300 g'),
('Galletas', 'Galletas integrales 250 g',    1.85, 19, '250 g'),
('Galletas', 'Tostadas de arroz 200 g',      1.70, 33, '200 g'),
('Galletas', 'Galletas de limon 300 g',      1.90,  3, '300 g'),
-- Lacteos
('Lacteos', 'Leche entera 1 L',              1.55, 40, '1 L'),
('Lacteos', 'Yogur natural 1 kg',            3.60, 18, '1 kg'),
('Lacteos', 'Leche descremada 1 L',          1.60, 36, '1 L'),
('Lacteos', 'Queso fresco 500 g',            4.80,  5, '500 g'),
('Lacteos', 'Manteca 200 g',                 2.45, 21, '200 g'),
('Lacteos', 'Crema de leche 200 ml',         1.75, 17, '200 ml'),
('Lacteos', 'Yogur de frutilla 1 kg',        3.80, 15, '1 kg'),
-- Limpieza
('Limpieza', 'Detergente liquido 750 ml',    2.90, 42, '750 ml'),
('Limpieza', 'Lavandina 1 L',                1.20, 50, '1 L'),
('Limpieza', 'Papel higienico x4',           2.15, 64, '4 rollos'),
('Limpieza', 'Esponja multiuso x3',          1.35, 37, '3 unidades');

-- Id estable: MD5 de "categoria/nombre" convertido a guid, en texto y minúsculas
DECLARE @conId TABLE (Id NVARCHAR(36), Categoria NVARCHAR(100), Nombre NVARCHAR(200), Precio DECIMAL(18, 2), Stock INT, Presentacion NVARCHAR(100));

INSERT INTO @conId
SELECT LOWER(CONVERT(NVARCHAR(36), CONVERT(UNIQUEIDENTIFIER, HASHBYTES('MD5', CONCAT(Categoria, '/', Nombre))))),
       Categoria, Nombre, Precio, Stock, Presentacion
FROM @p;

INSERT INTO dbo.Productos (Id, Categoria, Nombre, Precio, Stock)
SELECT Id, Categoria, Nombre, Precio, Stock FROM @conId c
WHERE NOT EXISTS (SELECT 1 FROM dbo.Productos p WHERE p.Id = c.Id);

INSERT INTO dbo.ProductoAtributos (ProductoId, Parametro, Valor)
SELECT Id, 'Presentacion', Presentacion FROM @conId c
WHERE NOT EXISTS (SELECT 1 FROM dbo.ProductoAtributos a WHERE a.ProductoId = c.Id AND a.Parametro = 'Presentacion');

SELECT Categoria, COUNT(*) AS Productos, SUM(CASE WHEN Stock = 0 THEN 1 ELSE 0 END) AS SinStock
FROM dbo.Productos GROUP BY Categoria ORDER BY Categoria;
GO

PRINT 'Base CatalogoDb lista.';
GO
