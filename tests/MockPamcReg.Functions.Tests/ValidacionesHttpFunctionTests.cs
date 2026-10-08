using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using MockPamcReg.Functions.Functions;
using MockPamcReg.Functions.Tests.Fakes;
using Xunit;

namespace MockPamcReg.Functions.Tests;

public sealed class ValidacionesHttpFunctionTests
{
    private const string Json200 = """
        {
            "correlacion_id": "20665a5d-80f2-42f6-a71b-3b81a72a2d50",
            "codigo_estado": 200,
            "descripcion": "Transacción Exitosa",
            "estadoConsulta": {
                "numeroControl": "5218067652",
                "codError": "0",
                "descripcionError": "OK",
                "fechaHoraConsulta": "2026-09-16 10:00:37"
            }
        }
        """;

    private const string Json209 = """
        {
            "correlacion_id": "2e12c694-22e9-44e3-a22a-7b6c1192b35c",
            "codigo_estado": 209,
            "descripcion": "Transacción Exitosa"
        }
        """;

    private static ValidacionesHttpFunction CrearFuncion(
        Dictionary<string, string?> documentos)
    {
        return new ValidacionesHttpFunction(
            new FakeRegistraduriaMockRepository(documentos),
            NullLogger<ValidacionesHttpFunction>.Instance);
    }

    private static HttpRequest CrearRequest(
        params (string Clave, string Valor)[] parametros)
    {
        var contexto = new DefaultHttpContext();

        contexto.Request.QueryString = QueryString.Create(
            parametros.Select(p =>
                new KeyValuePair<string, string?>(p.Clave, p.Valor)));

        return contexto.Request;
    }

    [Fact]
    public async Task DevuelveElJsonTalCual_CuandoExisteElDocumento()
    {
        var funcion = CrearFuncion(new()
        {
            ["1151957777"] = Json200
        });

        var respuesta = await funcion.Validaciones(
            CrearRequest(
                ("numero_identificacion", "1151957777"),
                ("codigo_tipo_identificacion", "1")),
            CancellationToken.None);

        var contenido = Assert.IsType<ContentResult>(respuesta);
        Assert.Equal(Json200, contenido.Content);
        Assert.Equal("application/json; charset=utf-8", contenido.ContentType);
    }

    [Fact]
    public async Task UsaCodigoEstadoDelJsonComoCodigoHttp()
    {
        var funcion = CrearFuncion(new()
        {
            ["1151957777"] = Json200
        });

        var respuesta = await funcion.Validaciones(
            CrearRequest(("numero_identificacion", "1151957777")),
            CancellationToken.None);

        var contenido = Assert.IsType<ContentResult>(respuesta);
        Assert.Equal(200, contenido.StatusCode);
    }

    [Fact]
    public async Task Devuelve209_CuandoElJsonTrae209()
    {
        var funcion = CrearFuncion(new()
        {
            ["52098280"] = Json209
        });

        var respuesta = await funcion.Validaciones(
            CrearRequest(("numero_identificacion", "52098280")),
            CancellationToken.None);

        var contenido = Assert.IsType<ContentResult>(respuesta);
        Assert.Equal(209, contenido.StatusCode);
        Assert.Equal(Json209, contenido.Content);
    }

    [Fact]
    public async Task Devuelve404_CuandoNoExisteElDocumento()
    {
        var funcion = CrearFuncion(new());

        var respuesta = await funcion.Validaciones(
            CrearRequest(("numero_identificacion", "0000000")),
            CancellationToken.None);

        var objeto = Assert.IsType<ObjectResult>(respuesta);
        Assert.Equal(404, objeto.StatusCode);
    }

    [Fact]
    public async Task Devuelve400_CuandoFaltaNumeroIdentificacion()
    {
        var funcion = CrearFuncion(new());

        var respuesta = await funcion.Validaciones(
            CrearRequest(("codigo_tipo_identificacion", "1")),
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(respuesta);
    }

    [Fact]
    public async Task IgnoraHeadersDeApim_NoAfectaLaRespuesta()
    {
        var funcion = CrearFuncion(new()
        {
            ["1151957777"] = Json200
        });

        var request = CrearRequest(("numero_identificacion", "1151957777"));
        request.Headers["Ocp-Apim-Subscription-Key"] = "cualquier-valor";
        request.Headers["Authorization"] = "Bearer token-invalido";
        request.Headers["correlacion_id"] = Guid.NewGuid().ToString();

        var respuesta = await funcion.Validaciones(
            request, CancellationToken.None);

        var contenido = Assert.IsType<ContentResult>(respuesta);
        Assert.Equal(200, contenido.StatusCode);
    }

    [Fact]
    public async Task Usa200_CuandoElJsonNoTraeCodigoEstado()
    {
        var funcion = CrearFuncion(new()
        {
            ["1"] = "{\"descripcion\":\"sin codigo\"}"
        });

        var respuesta = await funcion.Validaciones(
            CrearRequest(("numero_identificacion", "1")),
            CancellationToken.None);

        var contenido = Assert.IsType<ContentResult>(respuesta);
        Assert.Equal(200, contenido.StatusCode);
    }

    [Fact]
    public void HealthDevuelveStatusHealthy()
    {
        var funcion = new HealthCheckFunction();

        var respuesta = funcion.Health(new DefaultHttpContext().Request);

        var ok = Assert.IsType<OkObjectResult>(respuesta);
        var json = JsonSerializer.Serialize(ok.Value);
        Assert.Contains("\"status\":\"Healthy\"", json);
    }
}
