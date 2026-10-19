# Id de productos

Con imágenes en Blob Storage:

```
0157a18c-b219-4eaf-9f3f-a5d141b2b88c   Producto 1       Almacén
8bdab48c-8430-4597-8abd-0a79dc4ca6cb   Producto 10001   Almacén
98571078-0728-42d2-be5c-642b628fa7e0   Producto 100     Bebidas
d70cc85a-3ee1-40de-aa9f-2497660f5c4d   Producto 1000    Bebidas
d11fe81a-bcef-4323-adfc-b3b8be6eb822   Producto 10003   Lácteos
b90750c0-92eb-4ed6-812a-0d2cf73ffa79   Producto 10007   Lácteos
4f2945dc-42e9-4ff2-8ea2-d863d3f78ef6   Producto 10      Limpieza
d93e16d4-50ce-41b8-9f5e-727da7f39bd1   Producto 10002   Limpieza
```

Sin imágenes (para subir):

```
38f72aab-14a0-4c36-9d8a-a82162dadf32   Producto 10005   Almacén
f18701b6-042c-448b-99e0-8b8728a2c0d2   Producto 10004   Bebidas
77f01d8b-f9a7-498d-af79-f45a07050f4b   Producto 10011   Lácteos
be97166c-6e3b-48b5-ac1c-243516dadd0b   Producto 10006   Limpieza
```

Cosmos DB (`POST /api/cosmos/valoraciones`, luego `GET /api/cosmos/valoraciones/{productoId}`): ver `cosmos-01-valoracion-valida.json`, `cosmos-02-valoracion-sin-comentario.json` (201) y `cosmos-03-invalido-400-puntaje.json` (400). Usan los Id de productos de arriba.
