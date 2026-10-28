using Microsoft.AspNetCore.Mvc;

namespace Encurtador.Api.Controllers;

// Exemplo de referência: um controller mínimo.
// [ApiController] liga validação automática e respostas 400 padronizadas.
// [Route("health")] define a URL base de todas as actions desta classe.
[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    // GET /health → 200 OK com um JSON simples.
    // Ok(...) serializa o objeto anônimo para JSON automaticamente.
    [HttpGet]
    public IActionResult Get() => Ok(new { status = "ok" });
}
