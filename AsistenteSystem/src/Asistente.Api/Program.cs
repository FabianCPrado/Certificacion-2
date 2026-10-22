using Asistente.Api.Endpoints;
using Asistente.Api.Infrastructure;
using Asistente.Application;
using Asistente.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration, Path.Combine(AppContext.BaseDirectory, "mapa-sistema.json"));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();                                                          // /openapi/v1.json
    app.UseSwaggerUI(o => o.SwaggerEndpoint("/openapi/v1.json", "Asistente")); // en desarrollo la API queda abierta
}

app.UseHttpsRedirection();

app.MapAsistente();

await app.RunAsync();

// Necesario para WebApplicationFactory en los tests de integración
public partial class Program
{
    protected Program()
    {
    }
}
