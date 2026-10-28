using Microsoft.EntityFrameworkCore;
using Npgsql;
using UrlShortener.Api.Data;
using UrlShortener.Api.Models;

namespace UrlShortener.Api.Repositories;

// Registrado como Scoped: um DbContext por requisição, porque o DbContext não é thread-safe.
public class EfLinkRepository(AppDbContext db) : ILinkRepository
{
    public async Task<bool> TryAddAsync(Link link, CancellationToken cancellationToken = default)
    {
        db.Links.Add(link);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Código repetido: o índice único recusou. Tira o link do rastreamento do EF,
            // senão o próximo SaveChanges tentaria gravá-lo de novo.
            db.Entry(link).State = EntityState.Detached;
            return false;
        }
    }

    // AsNoTracking: só leitura, o EF não precisa acompanhar mudanças neste objeto.
    public Task<Link?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        db.Links.AsNoTracking().FirstOrDefaultAsync(l => l.Code == code, cancellationToken);
}
