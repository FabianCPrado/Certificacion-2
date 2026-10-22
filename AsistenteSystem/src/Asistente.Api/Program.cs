using System.Text.Json.Nodes;
using Asistente.Api.Endpoints;
using Asistente.Api.Infrastructure;
using Asistente.Application;
using Asistente.Application.Asistente;
using Asistente.Application.Sistemas;
using Asistente.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(
        builder.Configuration,
        Path.Combine(AppContext.BaseDirectory, "mapa-sistema.json"),
        Path.Combine(AppContext.BaseDirectory, "sistemas.json"),
        Path.Combine(AppContext.BaseDirectory, "conocimiento"));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
// Ejemplo del cuerpo de /preguntar en Swagger: un cliente con pedidos (Swagger:ClienteEjemplo), para probar sin buscar ids
var clienteEjemplo = builder.Configuration["Swagger:ClienteEjemplo"];
builder.Services.AddOpenApi(opciones => opciones.AddSchemaTransformer((esquema, contexto, _) =>
{
    if (contexto.JsonTypeInfo.Type == typeof(PreguntaRequest))
    {
        esquema.Examples =
        [
            new JsonObject
            {
                ["pregunta"] = "¿Qué productos compré en mis últimos pedidos?",
                ["clienteId"] = string.IsNullOrWhiteSpace(clienteEjemplo) ? null : clienteEjemplo
            }
        ];
    }

    // Ejemplo del cuerpo de /api/sistemas/preguntar
    if (contexto.JsonTypeInfo.Type == typeof(PreguntaSistemasRequest))
    {
        esquema.Examples =
        [
            new JsonObject { ["pregunta"] = "¿Qué se rompe si cambio el DTO de productos de Catálogo?" }
        ];
    }

    return Task.CompletedTask;
}));

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();                                                          // /openapi/v1.json
    app.UseSwaggerUI(o => o.SwaggerEndpoint("/openapi/v1.json", "Asistente")); // en desarrollo la API queda abierta
}

app.UseHttpsRedirection();

app.MapAsistente();
app.MapSistemas();

await app.RunAsync();

// Necesario para WebApplicationFactory en los tests de integración
public partial class Program
{
    protected Program()
    {
    }
}
