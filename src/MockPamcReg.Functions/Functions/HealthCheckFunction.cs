using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace MockPamcReg.Functions.Functions;

/// <summary>
/// Health check del mock, espeja la ruta real:
/// <c>GET /apivalidaciones/v1.0.0/health</c>.
/// </summary>
public sealed class HealthCheckFunction
{
    private const string Ruta =
        "apivalidaciones/v1.0.0/health";

    [Function("health")]
    public IActionResult Health(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = Ruta)]
        HttpRequest request)
    {
        return new OkObjectResult(new { status = "Healthy" });
    }
}
