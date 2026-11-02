using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using UrlShortener.Api.Dtos;

namespace UrlShortener.Api.Tests;

[Collection(ApiCollection.Name)]
public class RateLimitingTests(ApiFactory factory)
{
    // Uma API nova por teste, com limites baixos e contadores zerados.
    private HttpClient CreateClient(int createPerMinute, int requestsPerMinute) =>
        factory.WithWebHostBuilder(builder => builder
                .UseSetting("RateLimiting:CreatePerMinute", createPerMinute.ToString())
                .UseSetting("RateLimiting:RequestsPerMinute", requestsPerMinute.ToString()))
            .CreateClient();

    private static Task<HttpResponseMessage> CreateLink(HttpClient client, string? clientIp = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/links")
        {
            Content = JsonContent.Create(new CreateLinkRequest("https://example.com/page")),
        };
        if (clientIp is not null)
        {
            request.Headers.Add("X-Forwarded-For", clientIp);
        }

        return client.SendAsync(request);
    }

    [Fact]
    public async Task Post_OverCreateLimit_Returns429WithMessage()
    {
        var client = CreateClient(createPerMinute: 2, requestsPerMinute: 100);

        Assert.Equal(HttpStatusCode.Created, (await CreateLink(client)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await CreateLink(client)).StatusCode);
        var response = await CreateLink(client);

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Too many requests. Please try again in a minute.", problem?.Title);
    }

    [Fact]
    public async Task Post_FromAnotherIp_HasItsOwnLimit()
    {
        var client = CreateClient(createPerMinute: 1, requestsPerMinute: 100);

        Assert.Equal(HttpStatusCode.Created, (await CreateLink(client, "203.0.113.1")).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await CreateLink(client, "203.0.113.1")).StatusCode);

        Assert.Equal(HttpStatusCode.Created, (await CreateLink(client, "203.0.113.2")).StatusCode);
    }

    [Fact]
    public async Task Get_OverGlobalLimit_Returns429()
    {
        var client = CreateClient(createPerMinute: 100, requestsPerMinute: 3);

        for (var i = 0; i < 3; i++)
        {
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.GetAsync("/health")).StatusCode);
    }
}
