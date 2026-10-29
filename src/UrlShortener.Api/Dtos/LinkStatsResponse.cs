namespace UrlShortener.Api.Dtos;

public record LinkStatsResponse(string Code, int TotalClicks, IReadOnlyList<DailyClicksResponse> Daily);

public record DailyClicksResponse(DateOnly Date, int Clicks);
