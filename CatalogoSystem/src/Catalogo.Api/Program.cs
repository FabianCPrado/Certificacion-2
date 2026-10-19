using Catalogo.Api.Endpoints;
using Catalogo.Api.Grpc;
using Catalogo.Api.Infrastructure;
using Catalogo.Application;
using Catalogo.Infrastructure;
using Catalogo.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Capas: Application (MediatR + validadores) e Infrastructure (SQL Server)
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

// API: errores como ProblemDetails, documentación OpenAPI y gRPC
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.AddGrpc(o => o.Interceptors.Add<LoggingInterceptor>());

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();                                                         // /openapi/v1.json
    app.UseSwaggerUI(o => o.SwaggerEndpoint("/openapi/v1.json", "Catálogo"));
    app.MapDemoClase6();
    await app.Services.InicializarBaseDatosAsync();   // crea CatalogoDb y sus tablas; si SQL Server no está, solo avisa
}

app.UseHttpsRedirection();

app.MapProductos();                      // puerta REST (Minimal APIs)
app.MapGrpcService<CatalogoGrpcService>();   // puerta gRPC: mismo caso de uso

await app.RunAsync();
