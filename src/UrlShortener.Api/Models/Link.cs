namespace UrlShortener.Api.Models;

// Entidade de domínio: é o que fica guardado. Não sai direto pela API (ver Dtos/).
public class Link
{
    public required string Code { get; init; }
    public required string OriginalUrl { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
