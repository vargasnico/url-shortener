using Amazon.Lambda.AspNetCoreServer.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using UrlShortener.Api.Data;
using UrlShortener.Api.Repositories;
using UrlShortener.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Na AWS a API roda dentro de uma Lambda atrás do API Gateway (HTTP API).
// Fora da Lambda (local, testes) esta linha não faz nada e o Kestrel continua normal.
builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);

// O CloudFront repassa o domínio público original neste cabeçalho (ver infra/template.yaml).
// Sem isso, o Request.Host seria o domínio interno do API Gateway e o shortUrl sairia errado.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedHost;
    options.ForwardedHostHeaderName = "X-Original-Host";
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

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
app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    // Documento OpenAPI em /openapi/v1.json
    app.MapOpenApi();
}

// Aplica migrations pendentes ao iniciar: sempre em Development e, em produção, só com
// Database__MigrateOnStartup=true. O EF Core usa um lock no banco, então várias instâncias
// subindo juntas não aplicam a mesma migration duas vezes.
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Torna a classe Program visível para os testes de integração (WebApplicationFactory<Program>).
public partial class Program { }
