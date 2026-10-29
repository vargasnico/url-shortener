namespace UrlShortener.Api.Models;

// Total de cliques de um link por dia (UTC). Escrito pelo worker, lido pela API.
public class LinkDailyStat
{
    public long LinkId { get; init; }
    public DateOnly Day { get; init; }
    public int Clicks { get; init; }
}
