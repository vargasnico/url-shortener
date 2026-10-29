using Microsoft.AspNetCore.Mvc;
using UrlShortener.Api.Dtos;
using UrlShortener.Api.Models;
using UrlShortener.Api.Repositories;
using UrlShortener.Api.Services;

namespace UrlShortener.Api.Controllers;

[ApiController]
[Route("links")]
public class LinksController(ILinkRepository repository, IClickRepository clicks, ICodeGenerator codeGenerator) : ControllerBase
{
    private const int MaxAttempts = 5;

    // POST /links
    [HttpPost]
    public async Task<ActionResult<LinkResponse>> Create(CreateLinkRequest request, CancellationToken cancellationToken)
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
            if (await repository.TryAddAsync(link, cancellationToken))
            {
                // 201 Created + header Location apontando para GET /links/{code}
                return CreatedAtAction(nameof(Get), new { code = link.Code }, ToResponse(link));
            }
        }

        return Problem("Could not generate a unique code.", statusCode: StatusCodes.Status500InternalServerError);
    }

    // GET /links/{code} → dados do link, sem redirecionar
    [HttpGet("{code}")]
    public async Task<ActionResult<LinkResponse>> Get(string code, CancellationToken cancellationToken)
    {
        var link = await repository.GetByCodeAsync(code, cancellationToken);
        return link is null ? NotFound() : ToResponse(link);
    }

    // GET /links/{code}/stats → cliques agregados pelo worker
    [HttpGet("{code}/stats")]
    public async Task<ActionResult<LinkStatsResponse>> GetStats(string code, CancellationToken cancellationToken)
    {
        var link = await repository.GetByCodeAsync(code, cancellationToken);
        if (link is null)
        {
            return NotFound();
        }

        var daily = await clicks.GetDailyStatsAsync(link.Id, cancellationToken);
        return new LinkStatsResponse(
            link.Code,
            daily.Sum(d => d.Clicks),
            daily.Select(d => new DailyClicksResponse(d.Day, d.Clicks)).ToList());
    }

    // GET /{code} → registra o clique e redireciona. A "/" inicial ignora o [Route("links")] da classe.
    [HttpGet("/{code}")]
    public async Task<IActionResult> RedirectToOriginal(string code, CancellationToken cancellationToken)
    {
        var link = await repository.GetByCodeAsync(code, cancellationToken);
        if (link is null)
        {
            return NotFound();
        }

        await clicks.RecordAsync(new ClickEvent
        {
            LinkId = link.Id,
            UserAgent = Truncate(Request.Headers.UserAgent.ToString(), ClickEvent.UserAgentMaxLength),
            Referrer = Truncate(Request.Headers.Referer.ToString(), ClickEvent.ReferrerMaxLength),
        }, cancellationToken);

        return Redirect(link.OriginalUrl);
    }

    // Cabeçalhos vêm do cliente: podem faltar ou ser enormes.
    private static string? Truncate(string value, int maxLength) =>
        string.IsNullOrEmpty(value) ? null : value[..Math.Min(value.Length, maxLength)];

    private static bool IsValidHttpUrl(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    // Monta a URL curta a partir da própria requisição, então funciona em localhost e em produção.
    private LinkResponse ToResponse(Link link) =>
        new(link.Code, $"{Request.Scheme}://{Request.Host}/{link.Code}", link.OriginalUrl, link.CreatedAt);
}
