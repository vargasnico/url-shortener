namespace UrlShortener.Api.Dtos;

public record LinkResponse(string Code, string ShortUrl, string OriginalUrl, DateTimeOffset CreatedAt);
