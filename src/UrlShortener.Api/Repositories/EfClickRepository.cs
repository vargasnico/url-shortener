using Microsoft.EntityFrameworkCore;
using UrlShortener.Api.Data;
using UrlShortener.Api.Models;

namespace UrlShortener.Api.Repositories;

public class EfClickRepository(AppDbContext db) : IClickRepository
{
    public async Task RecordAsync(ClickEvent click, CancellationToken cancellationToken = default)
    {
        db.ClickEvents.Add(click);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LinkDailyStat>> GetDailyStatsAsync(long linkId, CancellationToken cancellationToken = default) =>
        await db.LinkDailyStats
            .AsNoTracking()
            .Where(s => s.LinkId == linkId)
            .OrderByDescending(s => s.Day)
            .ToListAsync(cancellationToken);
}
