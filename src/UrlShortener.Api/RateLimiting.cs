using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace UrlShortener.Api;

// Limites por IP, configuráveis na seção "RateLimiting" (appsettings ou variáveis de ambiente,
// ex.: RateLimiting__CreatePerMinute=20 no Azure).
public class RateLimitSettings
{
    // Links criados por minuto: protege contra spam de links.
    public int CreatePerMinute { get; set; } = 10;

    // Qualquer requisição por minuto (redirects, stats...): protege a cota de CPU do plano grátis.
    public int RequestsPerMinute { get; set; } = 120;
}

public static class RateLimiting
{
    public const string CreatePolicy = "create";

    public static IServiceCollection AddLinkRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RateLimitSettings>(configuration.GetSection("RateLimiting"));

        services.AddRateLimiter(options =>
        {
            // Os limites são lidos na requisição (não aqui), para os testes conseguirem trocá-los.
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                PerMinute(ClientIp(context), Settings(context).RequestsPerMinute));

            options.AddPolicy(CreatePolicy, context =>
                PerMinute(ClientIp(context), Settings(context).CreatePerMinute));

            // 429 com "problem details": o front mostra o "title" como mensagem de erro.
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (rejection, cancellationToken) =>
            {
                if (rejection.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    rejection.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
                }

                await rejection.HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Too many requests. Please try again in a minute.",
                }, cancellationToken);
            };
        });

        return services;
    }

    // Janela fixa de 1 minuto, sem fila: passou do limite, recusa na hora.
    private static RateLimitPartition<string> PerMinute(string key, int permits) =>
        RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permits,
            Window = TimeSpan.FromMinutes(1),
        });

    private static RateLimitSettings Settings(HttpContext context) =>
        context.RequestServices.GetRequiredService<IOptions<RateLimitSettings>>().Value;

    // Atrás de proxy, o IP real vem do X-Forwarded-For (ver ForwardedHeaders no Program.cs).
    private static string ClientIp(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
