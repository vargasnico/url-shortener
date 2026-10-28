using System.Net;

namespace UrlShortener.Api.Tests;

// Teste de integração: sobe a API inteira em memória e faz requisições HTTP reais.
[Collection(ApiCollection.Name)]
public class HealthControllerTests(ApiFactory factory)
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
