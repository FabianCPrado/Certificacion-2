using Pedidos.Api.Endpoints;
using Pedidos.Api.Infrastructure;
using Pedidos.Api.Middleware;
using Pedidos.Application;
using Pedidos.Infrastructure;
using Pedidos.Infrastructure.Cosmos;

var builder = WebApplication.CreateBuilder(args);

// Capas: Application (MediatR + validadores) e Infrastructure (EF Core)
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

// API: controladores, errores como ProblemDetails y documentación OpenAPI
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddTransient<AuditoriaMiddleware>();   // Transient: una instancia nueva por request
builder.Services.AddOpenApi();

var app = builder.Build();

// Primero la auditoría, para que envuelva al manejador de errores y registre el código final de la respuesta
app.UseMiddleware<AuditoriaMiddleware>();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Pedidos API v1"));

    // Demos de la Clase 4 (EF Core, LINQ y Dapper): solo en Development
    app.MapDemoClase4();

    // Demos de la Clase 5 (Redis, Blob Storage y Cosmos DB): solo en Development
    app.MapDemoClase5();
    app.MapDemoClase6();
    await app.Services.InicializarCosmosAsync();   // crea los contenedores de Cosmos; si el emulador no está, solo avisa
}

app.UseHttpsRedirection();

app.MapControllers();

await app.RunAsync();
