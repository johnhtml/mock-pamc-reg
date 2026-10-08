using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using MockPamcReg.Functions.Storage;

namespace MockPamcReg.Functions.Functions;

/// <summary>
/// Mock del endpoint real de Registraduría:
/// <c>GET /apivalidaciones/v1.0.0/validaciones?numero_identificacion={n}&amp;codigo_tipo_identificacion={t}</c>.
///
/// La respuesta se lee de un blob <c>{numero_identificacion}.json</c> y se
/// devuelve tal cual (sin re-serializar). El código HTTP de la respuesta es el
/// valor del campo <c>codigo_estado</c> contenido en el JSON.
///
/// La autenticación de API Management se acepta pero NO se valida: la función
/// es pública (AuthorizationLevel.Anonymous).
/// </summary>
public sealed class ValidacionesHttpFunction
{
    private const string Ruta =
        "apivalidaciones/v1.0.0/validaciones";

    private readonly IRegistraduriaMockRepository _repositorio;
    private readonly ILogger<ValidacionesHttpFunction> _logger;

    public ValidacionesHttpFunction(
        IRegistraduriaMockRepository repositorio,
        ILogger<ValidacionesHttpFunction> logger)
    {
        _repositorio = repositorio;
        _logger = logger;
    }

    [Function("validaciones")]
    public async Task<IActionResult> Validaciones(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = Ruta)]
        HttpRequest request,
        CancellationToken ct)
    {
        // TODO(deuda): La autenticación de APIM se acepta pero no se valida.
        // Los headers Ocp-Apim-Subscription-Key / Authorization se ignoran
        // intencionalmente (mock público).

        var numeroIdentificacion =
            request.Query["numero_identificacion"].ToString().Trim();

        if (string.IsNullOrWhiteSpace(numeroIdentificacion))
        {
            _logger.LogWarning(
                "Petición de validación sin numero_identificacion.");

            return new BadRequestObjectResult(new
            {
                codigo_estado = StatusCodes.Status400BadRequest,
                descripcion =
                    "El parámetro numero_identificacion es obligatorio."
            });
        }

        string? contenido;

        try
        {
            contenido = await _repositorio.ObtenerDocumentoAsync(
                numeroIdentificacion,
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error leyendo el blob mock del documento {Documento}.",
                numeroIdentificacion);

            return new ObjectResult(new
            {
                codigo_estado =
                    StatusCodes.Status502BadGateway,
                descripcion =
                    "Error consultando el almacenamiento de respuestas mock."
            })
            {
                StatusCode = StatusCodes.Status502BadGateway
            };
        }

        if (contenido is null)
        {
            // TODO(deuda): No se conoce el body del escenario "documento no
            // encontrado" de la API real de Registraduría. Se deja un
            // placeholder explícito hasta contar con el contrato real.
            // Ver docs/pamc/mock-registraduria.md.
            _logger.LogInformation(
                "No existe blob mock para el documento {Documento}.",
                numeroIdentificacion);

            return new ObjectResult(new
            {
                codigo_estado = StatusCodes.Status404NotFound,
                descripcion =
                    "TODO: definir body de documento no encontrado " +
                    "(contrato pendiente con Registraduría)."
            })
            {
                StatusCode = StatusCodes.Status404NotFound
            };
        }

        var codigoEstado = ExtraerCodigoEstado(contenido);

        return new ContentResult
        {
            Content = contenido,
            ContentType = "application/json; charset=utf-8",
            StatusCode = codigoEstado
        };
    }

    /// <summary>
    /// Obtiene el valor de <c>codigo_estado</c> del JSON mock para usarlo como
    /// código HTTP de la respuesta. Si no está presente o no es numérico,
    /// devuelve 200.
    /// </summary>
    private static int ExtraerCodigoEstado(string json)
    {
        try
        {
            using var documento = JsonDocument.Parse(json);

            if (documento.RootElement.TryGetProperty(
                    "codigo_estado",
                    out var elemento)
                && elemento.ValueKind == JsonValueKind.Number
                && elemento.TryGetInt32(out var codigo))
            {
                return codigo;
            }
        }
        catch (JsonException)
        {
            // El mock se devuelve tal cual; si el JSON es inválido se asume 200.
        }

        return StatusCodes.Status200OK;
    }
}
