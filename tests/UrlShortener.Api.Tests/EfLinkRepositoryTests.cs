using Microsoft.Extensions.DependencyInjection;
using UrlShortener.Api.Models;
using UrlShortener.Api.Repositories;

namespace UrlShortener.Api.Tests;

[Collection(ApiCollection.Name)]
public class EfLinkRepositoryTests(ApiFactory factory)
{
    [Fact]
    public async Task TryAddAsync_WithDuplicateCode_ReturnsFalse()
    {
        // Cada scope simula uma requisição diferente (DbContexts separados).
        using var firstScope = factory.Services.CreateScope();
        using var secondScope = factory.Services.CreateScope();
        var first = firstScope.ServiceProvider.GetRequiredService<ILinkRepository>();
        var second = secondScope.ServiceProvider.GetRequiredService<ILinkRepository>();

        var added = await first.TryAddAsync(new Link { Code = "dupl01", OriginalUrl = "https://example.com/a" });
        var duplicate = await second.TryAddAsync(new Link { Code = "dupl01", OriginalUrl = "https://example.com/b" });

        Assert.True(added);
        Assert.False(duplicate);
        Assert.Equal("https://example.com/a", (await second.GetByCodeAsync("dupl01"))?.OriginalUrl);
    }
}
