# Almacenamiento por servicio

Decisión de dónde se guarda cada dato del proyecto **Pedidos** y por qué (tarea 5.1 de la Clase 5). El detalle técnico de cada base está en [`clase5-bases-de-datos.md`](clase5-bases-de-datos.md).

| Servicio / dato | Dónde | Por qué (una línea) |
|---|---|---|
| Pedidos, líneas y stock | SQL Server | Transacciones: el pedido, sus líneas y el descuento de stock se guardan juntos o no se guarda nada |
| Catálogo de productos | Cosmos DB, partition key `/categoria` | Atributos distintos por categoría (litros, pulgadas, talle…) y casi siempre se lee filtrando por categoría |
| Valoraciones de productos | Cosmos DB, partition key `/productoId` | Documentos que crecen sin parar y se leen siempre por producto |
| Lecturas de productos y pedidos | Redis delante de SQL (cache-aside) | Se leen mucho y cambian poco; expira solo (5 min en la app, 10 min en la demo) |
| Imágenes de productos | Blob Storage (Azurite en local) | Son archivos: la base guarda solo la URL |
| Auditoría de POST, PUT y DELETE | MongoDB | Solo se inserta; el cuerpo del request es JSON de forma libre |

## Cómo se decidió

- **¿Hay transacciones y relaciones fuertes?** → SQL Server (Pedidos).
- **¿La forma del dato cambia según el caso, o crece mucho y se lee por una clave?** → documentos (Cosmos DB o MongoDB).
- **¿Se lee mucho lo mismo y puede estar unos minutos desactualizado?** → Redis delante de la base, siempre con expiración y con invalidación al actualizar.
- **¿Es un archivo?** → Blob Storage, nunca dentro de la base.

## Partition key del catálogo: `/categoria` y no `/id`

- Con `/categoria`, listar los productos de una categoría se resuelve en **una sola partición** (barato y rápido), y el catálogo se consulta casi siempre así.
- Con `/id` cada producto sería su propia partición: la lectura por id sería perfecta, pero cualquier listado cruzaría todas las particiones.
- Riesgo a vigilar: si una categoría concentra casi todos los productos, esa partición se vuelve "caliente".
