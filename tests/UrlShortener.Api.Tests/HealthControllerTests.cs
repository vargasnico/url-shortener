using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace UrlShortener.Api.Tests;

// Teste de integração: sobe a API inteira em memória e faz requisições HTTP reais.
// IClassFixture compartilha a mesma API entre os testes desta classe.
public class HealthControllerTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Get_ReturnsOk()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"status\":\"ok\"", await response.Content.ReadAsStringAsync());
    }
}
