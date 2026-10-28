using Microsoft.AspNetCore.Mvc;
using UrlShortener.Api.Dtos;
using UrlShortener.Api.Models;
using UrlShortener.Api.Repositories;
using UrlShortener.Api.Services;

namespace UrlShortener.Api.Controllers;

[ApiController]
[Route("links")]
public class LinksController(ILinkRepository repository, ICodeGenerator codeGenerator) : ControllerBase
{
    private const int MaxAttempts = 5;

    // POST /links
    [HttpPost]
    public ActionResult<LinkResponse> Create(CreateLinkRequest request)
    {
        if (!IsValidHttpUrl(request.Url))
        {
            ModelState.AddModelError(nameof(request.Url), "Provide an absolute URL starting with http:// or https://.");
            return ValidationProblem();
        }

        // Colisão de código é rara (62^6 ≈ 56 bilhões de combinações), mas possível: tenta de novo.
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var link = new Link { Code = codeGenerator.Generate(), OriginalUrl = request.Url };
            if (repository.TryAdd(link))
            {
                // 201 Created + header Location apontando para GET /links/{code}
                return CreatedAtAction(nameof(Get), new { code = link.Code }, ToResponse(link));
            }
        }

        return Problem("Could not generate a unique code.", statusCode: StatusCodes.Status500InternalServerError);
    }

    // GET /links/{code} → dados do link, sem redirecionar
    [HttpGet("{code}")]
    public ActionResult<LinkResponse> Get(string code)
    {
        var link = repository.GetByCode(code);
        return link is null ? NotFound() : ToResponse(link);
    }

    // GET /{code} → redireciona. A "/" inicial ignora o [Route("links")] da classe.
    [HttpGet("/{code}")]
    public IActionResult RedirectToOriginal(string code)
    {
        var link = repository.GetByCode(code);
        return link is null ? NotFound() : Redirect(link.OriginalUrl);
    }

    private static bool IsValidHttpUrl(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    // Monta a URL curta a partir da própria requisição, então funciona em localhost e em produção.
    private LinkResponse ToResponse(Link link) =>
        new(link.Code, $"{Request.Scheme}://{Request.Host}/{link.Code}", link.OriginalUrl, link.CreatedAt);
}
