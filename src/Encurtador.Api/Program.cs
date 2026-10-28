var builder = WebApplication.CreateBuilder(args);

// Registro de serviços (injeção de dependência).
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// Pipeline HTTP: a ordem dos "Use..." importa, cada um é um middleware.
if (app.Environment.IsDevelopment())
{
    // Documento OpenAPI em /openapi/v1.json
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Torna a classe Program visível para os testes de integração (WebApplicationFactory<Program>).
public partial class Program { }
