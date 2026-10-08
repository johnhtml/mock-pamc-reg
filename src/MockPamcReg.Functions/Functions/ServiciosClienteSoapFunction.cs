using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using MockPamcReg.Functions.Soap;

namespace MockPamcReg.Functions.Functions;

/// <summary>
/// Mock del servicio SOAP de Gestión Clientes:
/// <c>POST http://gclientespru.compensar.com/ServiciosCliente.svc</c>
/// (operación <c>MarcacionRequisitosRegistraduria</c>).
///
/// Toma TODOS los <c>Requisito</c> recibidos y simula que todos fueron
/// actualizados. La autenticación no se valida (mock público); se ignora el
/// header <c>SOAPAction</c>.
/// </summary>
public sealed class ServiciosClienteSoapFunction
{
    private const string Ruta = "ServiciosCliente.svc";

    private readonly ILogger<ServiciosClienteSoapFunction> _logger;

    public ServiciosClienteSoapFunction(
        ILogger<ServiciosClienteSoapFunction> logger)
    {
        _logger = logger;
    }

    [Function("marcacionRequisitosRegistraduria")]
    public async Task<IActionResult> MarcacionRequisitosRegistraduria(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = Ruta)]
        HttpRequest request,
        CancellationToken ct)
    {
        var body = await LeerBodyAsync(request, ct);

        if (!MarcacionRequisitosRegistraduriaSoap.TryExtraerSiglas(
                body,
                out var siglas,
                out var error))
        {
            _logger.LogWarning(
                "Body SOAP inválido en ServiciosCliente.svc: {Error}",
                error);

            return new ContentResult
            {
                Content =
                    MarcacionRequisitosRegistraduriaSoap.ConstruirFault(
                        error ?? "Body SOAP inválido."),
                ContentType = "text/xml; charset=utf-8",
                StatusCode = StatusCodes.Status400BadRequest
            };
        }

        _logger.LogInformation(
            "Marcación mock de {Cantidad} requisitos: {Siglas}.",
            siglas.Count,
            string.Join(", ", siglas));

        return new ContentResult
        {
            Content =
                MarcacionRequisitosRegistraduriaSoap.ConstruirRespuesta(
                    siglas),
            ContentType = "text/xml; charset=utf-8",
            StatusCode = StatusCodes.Status200OK
        };
    }

    private static async Task<string> LeerBodyAsync(
        HttpRequest request,
        CancellationToken ct)
    {
        using var reader = new StreamReader(
            request.Body,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true);

        return await reader.ReadToEndAsync(ct);
    }
}
