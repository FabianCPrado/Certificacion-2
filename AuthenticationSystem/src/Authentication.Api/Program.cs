using Authentication.Api.Endpoints;
using Authentication.Infrastructure;
using Authentication.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthenticationInfrastructure(builder.Configuration);
builder.Services.AddOpenApi();
builder.Services.AddValidation();
builder.Services.AddProblemDetails();

var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Authentication API"));
    await app.Services.InitializeAuthenticationDbAsync(app.Configuration);
}

app.UseHttpsRedirection();
app.MapTokenEndpoints();
await app.RunAsync();