using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using UrlShortener.Api.Dtos;

namespace UrlShortener.Api.Tests;

[Collection(ApiCollection.Name)]
public class LinksControllerTests(ApiFactory factory)
{
    // Sem seguir redirects: queremos ver o 302 em si, não a página de destino.
    private readonly HttpClient _client = factory.CreateClient(
        new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    [Fact]
    public async Task Post_WithValidUrl_Returns201WithCodeAndShortUrl()
    {
        var response = await _client.PostAsJsonAsync("/links", new CreateLinkRequest("https://example.com/page"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var link = await response.Content.ReadFromJsonAsync<LinkResponse>();
        Assert.NotNull(link);
        Assert.Matches("^[a-zA-Z0-9]{6}$", link.Code);
        Assert.EndsWith($"/{link.Code}", link.ShortUrl);
        Assert.Equal("https://example.com/page", link.OriginalUrl);
        Assert.Equal($"/links/{link.Code}", response.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task Post_BehindCloudFront_UsesPublicHostInShortUrl()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/links")
        {
            Content = JsonContent.Create(new CreateLinkRequest("https://example.com/page")),
        };
        request.Headers.Add("X-Original-Host", "short.example");

        var response = await _client.SendAsync(request);

        var link = await response.Content.ReadFromJsonAsync<LinkResponse>();
        Assert.StartsWith("http://short.example/", link?.ShortUrl);
    }

    [Theory]
    [InlineData("")]
    [InlineData("example.com")]
    [InlineData("ftp://example.com")]
    [InlineData("javascript:alert(1)")]
    public async Task Post_WithInvalidUrl_Returns400(string url)
    {
        var response = await _client.PostAsJsonAsync("/links", new CreateLinkRequest(url));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetCode_Existing_Redirects302ToOriginalUrl()
    {
        var code = await CreateLink("https://example.com/target");

        var response = await _client.GetAsync($"/{code}");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("https://example.com/target", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task GetCode_Missing_Returns404()
    {
        var response = await _client.GetAsync("/missing");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetLink_Existing_ReturnsDetailsWithoutRedirecting()
    {
        var code = await CreateLink("https://example.com/details");

        var link = await _client.GetFromJsonAsync<LinkResponse>($"/links/{code}");

        Assert.Equal("https://example.com/details", link?.OriginalUrl);
    }

    private async Task<string> CreateLink(string url)
    {
        var response = await _client.PostAsJsonAsync("/links", new CreateLinkRequest(url));
        response.EnsureSuccessStatusCode();
        var link = await response.Content.ReadFromJsonAsync<LinkResponse>();
        return link!.Code;
    }
}
