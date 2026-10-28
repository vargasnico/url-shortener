using System.Collections.Concurrent;
using UrlShortener.Api.Models;

namespace UrlShortener.Api.Repositories;

// Registrado como Singleton: uma única instância (e um único dicionário) para a API inteira.
// ConcurrentDictionary porque requisições simultâneas rodam em threads diferentes.
public class InMemoryLinkRepository : ILinkRepository
{
    private readonly ConcurrentDictionary<string, Link> _links = new();

    public bool TryAdd(Link link) => _links.TryAdd(link.Code, link);

    public Link? GetByCode(string code) => _links.GetValueOrDefault(code);
}
