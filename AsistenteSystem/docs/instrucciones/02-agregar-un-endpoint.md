# Agregar un endpoint

> Pasos para sumar un caso de uso a un sistema con Clean Architecture y CQRS, de adentro hacia afuera.

Ejemplo: agregar `GET /api/pedidos/detalle` en Pedidos (se hizo así).

## Pasos

1. **Decidí si es command o query.** Si solo lee, es una query.
2. **Domain** (si hace falta): agregá el dato o la regla en la entidad. Una lectura nueva suele no tocar Domain.
3. **Application:**
   - Creá la carpeta del caso de uso, por ejemplo `Pedidos/Queries/ListarPedidosDetalle`.
   - Escribí el `Query` (o `Command`), su `Validator` con FluentValidation y su `Handler`.
   - Si necesita datos nuevos, agregá el método a la interfaz de consultas (`IPedidosConsultas`) o al repositorio.
   - Si el DTO es nuevo, ponelo junto al caso de uso. Un DTO no lleva lógica.
4. **Infrastructure:** implementá el método nuevo de la interfaz. Para lecturas: `AsNoTracking` y `Select` directo al DTO, sin cargar entidades.
5. **Api:** agregá la ruta (controlador o Minimal API) que solo llama a `sender.Send(...)`. Declará los códigos de respuesta (200, 400, 404).
6. **Tests:**
   - Un test del validador y del handler en `Application.Tests`.
   - Si agregaste un método a una interfaz, actualizá los **fakes** de los tests que la implementan, o no van a compilar.
   - Si la consulta es de Infrastructure, un test en `Infrastructure.Tests`.
7. **Probalo de verdad:** levantá el sistema y llamalo desde Swagger o un `.http`. Una base desactualizada se nota recién ahí (por ejemplo, una columna que falta).
8. **Dejá todo al día:** agregá el endpoint a `sistemas.json` y, si otro sistema lo va a consumir, anotalo como contrato.

## Errores comunes

- Olvidar registrar la interfaz nueva en `InfrastructureServiceExtensions`.
- Poner la lógica en el controlador en vez de en la entidad o el handler.
- Devolver una entidad en vez de un DTO.
- Aceptar un `tamanio` sin límite: el máximo es 100.
