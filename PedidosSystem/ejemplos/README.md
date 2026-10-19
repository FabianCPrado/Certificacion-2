# Ejemplos de pruebas

JSON para `POST /api/pedidos` (Swagger: `http://localhost:5101/swagger`).

| Archivo | Resultado esperado |
|---|---|
| `01-valido-basico.json` | 201 |
| `02-valido-varias-lineas-usd.json` | 201 |
| `03-valido-sin-codigo-postal.json` | 201 |
| `04-invalido-400-email-cantidad.json` | 400 |
| `05-invalido-400-sin-lineas.json` | 400 |
| `06-invalido-400-precio-negativo-calle-vacia.json` | 400 |
| `07-regla-negocio-409-moneda-minuscula.json` | 409 |

Con el id devuelto por un 201:

- `POST /api/pedidos/{id}/confirmar` → 204 (repetirlo → 409)
- `POST /api/pedidos/{id}/cancelar` → 204 (repetirlo → 409)
- `GET /api/pedidos/{id}` → 200 (id inexistente → 404)

Por consola (PowerShell):

```powershell
Invoke-RestMethod http://localhost:5101/api/pedidos -Method Post -ContentType application/json -InFile ejemplos/01-valido-basico.json
```

Id de productos para Blob Storage: ver `ids-productos.md`.
