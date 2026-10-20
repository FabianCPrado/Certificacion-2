using System.Text;
using Catalogo.Api.Endpoints;
using Catalogo.Api.Grpc;
using Catalogo.Api.Infrastructure;
using Catalogo.Application;
using Catalogo.Infrastructure;
using Catalogo.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Capas: Application (MediatR + validadores) e Infrastructure (SQL Server)
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

// API: errores como ProblemDetails, documentación OpenAPI y gRPC
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        var bearerScheme = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header
        };

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
        {
            ["Bearer"] = bearerScheme
        };

        var operations = document.Paths.Values
            .Select(path => path.Operations)
            .Where(pathOperations => pathOperations is not null)
            .SelectMany(pathOperations => pathOperations!.Values);

        foreach (var operation in operations)
        {
            operation.Security ??= [];
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = []
            });
        }

        return Task.CompletedTask;
    });
});
builder.Services.AddGrpc(o => o.Interceptors.Add<LoggingInterceptor>());

var jwtSigningKey = builder.Configuration["Jwt:SigningKey"];
if (string.IsNullOrWhiteSpace(jwtSigningKey) || Encoding.UTF8.GetByteCount(jwtSigningKey) < 32)
    throw new InvalidOperationException("Jwt:SigningKey debe tener al menos 32 bytes y configurarse fuera de appsettings.");

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "AuthenticationSystem";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "catalogo-api";
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddAuthorization(options => options.AddPolicy("PedidosService", policy =>
    policy.RequireAuthenticatedUser()
        .RequireClaim("client_id", "pedidos-service")
        .RequireClaim("client_type", "service")));

var app = builder.Build();

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

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
