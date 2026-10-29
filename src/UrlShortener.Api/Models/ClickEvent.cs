namespace UrlShortener.Api.Models;

// Um registro por redirecionamento. O worker de estatísticas lê os eventos ainda não
// agregados (AggregatedAt nulo), soma em LinkDailyStat e marca como agregados.
public class ClickEvent
{
    public const int UserAgentMaxLength = 512;
    public const int ReferrerMaxLength = 2048;

    public long Id { get; init; }
    public long LinkId { get; init; }
    public DateTimeOffset ClickedAt { get; init; } = DateTimeOffset.UtcNow;
    public string? UserAgent { get; init; }
    public string? Referrer { get; init; }
    public DateTimeOffset? AggregatedAt { get; init; }
}
