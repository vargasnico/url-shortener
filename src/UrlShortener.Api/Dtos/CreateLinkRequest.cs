using System.ComponentModel.DataAnnotations;

namespace UrlShortener.Api.Dtos;

// [Required] é checado automaticamente pelo [ApiController]: corpo sem "url" vira 400.
public record CreateLinkRequest([Required] string Url);
