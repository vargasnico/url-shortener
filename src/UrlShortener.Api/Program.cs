using Microsoft.EntityFrameworkCore;
using UrlShortener.Api.Data;
using UrlShortener.Api.Repositories;
using UrlShortener.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Registro de serviços (injeção de dependência).
// A connection string é lida quando o DbContext é criado (não aqui), para os testes
// conseguirem trocá-la pela do container de teste.
builder.Services.AddDbContext<AppDbContext>((services, options) =>
    options.UseNpgsql(services.GetRequiredService<IConfiguration>().GetConnectionString("Default")));

// Scoped: um repositório (e um DbContext) por requisição.
builder.Services.AddScoped<ILinkRepository, EfLinkRepository>();
builder.Services.AddScoped<IClickRepository, EfClickRepository>();
builder.Services.AddSingleton<ICodeGenerator, RandomCodeGenerator>();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// Pipeline HTTP: a ordem dos "Use..." importa, cada um é um middleware.
if (app.Environment.IsDevelopment())
{
    // Documento OpenAPI em /openapi/v1.json
    app.MapOpenApi();

    // Em desenvolvimento, aplica as migrations pendentes ao subir a API.
    // Em produção isso vira um passo separado do deploy (script SQL ou migration bundle).
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Torna a classe Program visível para os testes de integração (WebApplicationFactory<Program>).
public partial class Program { }
