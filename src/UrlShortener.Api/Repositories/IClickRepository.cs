using UrlShortener.Api.Models;

namespace UrlShortener.Api.Repositories;

public interface IClickRepository
{
    Task RecordAsync(ClickEvent click, CancellationToken cancellationToken = default);

    /// <returns>Estatísticas diárias do link, do dia mais recente para o mais antigo.</returns>
    Task<IReadOnlyList<LinkDailyStat>> GetDailyStatsAsync(long linkId, CancellationToken cancellationToken = default);
}
