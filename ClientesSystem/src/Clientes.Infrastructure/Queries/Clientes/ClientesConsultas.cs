using Microsoft.EntityFrameworkCore;
using Clientes.Application.Clientes.Queries.ObtenerCliente;
using Clientes.Application.Common;
using Clientes.Application.Interfaces.Clientes;
using Clientes.Infrastructure.Persistence;

namespace Clientes.Infrastructure.Queries.Clientes;

public sealed class ClientesConsultas(AppDbContext context) : IClienteConsultas
{
    public Task<ClienteDto?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Clientes.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new ClienteDto(c.Id, c.Nombre, c.Email))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<PagedResult<ClienteDto>> ListarAsync(
        int pagina, int tamanio, CancellationToken cancellationToken = default)
    {
        var total = await context.Clientes.CountAsync(cancellationToken);

        // AsNoTracking + proyección al DTO: solo viajan las columnas necesarias
        var items = await context.Clientes.AsNoTracking()
            .OrderBy(c => c.Nombre)
            .Skip((pagina - 1) * tamanio).Take(tamanio)
            .Select(c => new ClienteDto(c.Id, c.Nombre, c.Email))
            .ToListAsync(cancellationToken);

        return new PagedResult<ClienteDto>(items, pagina, tamanio, total);
    }
}
