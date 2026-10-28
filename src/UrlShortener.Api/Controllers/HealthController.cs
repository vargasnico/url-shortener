using Microsoft.AspNetCore.Mvc;

namespace UrlShortener.Api.Controllers;

// Health check: a infraestrutura (load balancer, ECS, App Runner) usa para saber se a API está viva.
[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    // GET /health → 200 OK com um JSON simples.
    [HttpGet]
    public IActionResult Get() => Ok(new { status = "ok" });
}
