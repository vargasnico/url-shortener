using UrlShortener.Api.Models;

namespace UrlShortener.Api.Repositories;

// O controller depende desta interface, não da implementação.
// Trocar o armazenamento (memória, banco) não exige mudar o controller.
public interface ILinkRepository
{
    /// <returns>false se já existir um link com o mesmo código.</returns>
    bool TryAdd(Link link);

    Link? GetByCode(string code);
}
