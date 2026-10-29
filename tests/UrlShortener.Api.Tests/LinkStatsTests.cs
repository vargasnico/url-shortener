using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UrlShortener.Api.Data;
using UrlShortener.Api.Dtos;
using UrlShortener.Api.Models;

namespace UrlShortener.Api.Tests;

[Collection(ApiCollection.Name)]
public class LinkStatsTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient(
        new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    [Fact]
    public async Task Redirect_RecordsPendingClickEvent()
    {
        var link = await CreateLink();

        var request = new HttpRequestMessage(HttpMethod.Get, $"/{link.Code}");
        request.Headers.UserAgent.ParseAdd("TestAgent/1.0");
        request.Headers.Referrer = new Uri("https://referrer.example/");
        await _client.SendAsync(request);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var linkId = await db.Links.Where(l => l.Code == link.Code).Select(l => l.Id).SingleAsync();
        var click = await db.ClickEvents.SingleAsync(c => c.LinkId == linkId);

        Assert.Equal("TestAgent/1.0", click.UserAgent);
        Assert.Equal("https://referrer.example/", click.Referrer);
        Assert.Null(click.AggregatedAt);
    }

    [Fact]
    public async Task GetStats_ReturnsDailyTotalsNewestFirst()
    {
        var link = await CreateLink();
        await SeedDailyStats(link.Code, (new DateOnly(2026, 10, 27), 3), (new DateOnly(2026, 10, 28), 5));

        var stats = await _client.GetFromJsonAsync<LinkStatsResponse>($"/links/{link.Code}/stats");

        Assert.NotNull(stats);
        Assert.Equal(8, stats.TotalClicks);
        Assert.Equal([new DailyClicksResponse(new DateOnly(2026, 10, 28), 5), new DailyClicksResponse(new DateOnly(2026, 10, 27), 3)], stats.Daily);
    }

    [Fact]
    public async Task GetStats_WithoutAggregatedClicks_ReturnsZero()
    {
        var link = await CreateLink();

        var stats = await _client.GetFromJsonAsync<LinkStatsResponse>($"/links/{link.Code}/stats");

        Assert.Equal(0, stats?.TotalClicks);
        Assert.Empty(stats!.Daily);
    }

    [Fact]
    public async Task GetStats_UnknownCode_Returns404()
    {
        var response = await _client.GetAsync("/links/missing/stats");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<LinkResponse> CreateLink()
    {
        var response = await _client.PostAsJsonAsync("/links", new CreateLinkRequest("https://example.com/stats"));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LinkResponse>())!;
    }

    // Simula o que o worker Python grava.
    private async Task SeedDailyStats(string code, params (DateOnly Day, int Clicks)[] days)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var linkId = await db.Links.Where(l => l.Code == code).Select(l => l.Id).SingleAsync();
        db.LinkDailyStats.AddRange(days.Select(d => new LinkDailyStat { LinkId = linkId, Day = d.Day, Clicks = d.Clicks }));
        await db.SaveChangesAsync();
    }
}
