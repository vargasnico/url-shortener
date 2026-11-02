using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using UrlShortener.Api;
using UrlShortener.Api.Data;
using UrlShortener.Api.Repositories;
using UrlShortener.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// No Azure App Service o HTTPS termina no balanceador e a app recebe HTTP puro.
// X-Forwarded-Proto: informa o esquema original, para o shortUrl sair com https://.
// X-Forwarded-For: IP real do cliente, usado no limite de requisições. Só a última entrada é usada
// (a que o proxy adicionou), então o cliente não consegue trocar de IP mandando o cabeçalho.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor;
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
builder.Services.AddLinkRateLimiting(builder.Configuration);
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

// No Azure o front (build do Vite) é publicado em wwwroot e servido pela própria API, num domínio só.
// Localmente não existe wwwroot e estes middlewares não fazem nada (o Vite serve o front).
app.UseDefaultFiles();
app.UseStaticFiles();

// Depois dos arquivos estáticos: carregar o front não conta no limite, só as chamadas à API.
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Torna a classe Program visível para os testes de integração (WebApplicationFactory<Program>).
public partial class Program { }
