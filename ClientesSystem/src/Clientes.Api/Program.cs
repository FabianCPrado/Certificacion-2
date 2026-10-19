using Clientes.Api.Infrastructure;
using Clientes.Application;
using Clientes.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Capas: Application (MediatR + validadores) e Infrastructure (EF Core)
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

// API: controladores, errores como ProblemDetails y documentación OpenAPI
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Clientes API v1"));
}

app.UseHttpsRedirection();

app.MapControllers();

await app.RunAsync();
