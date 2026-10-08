using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using MockPamcReg.Functions.Functions;
using MockPamcReg.Functions.Soap;
using Xunit;

namespace MockPamcReg.Functions.Tests;

public sealed class MarcacionRequisitosRegistraduriaSoapTests
{
    private const string RequestEjemplo = """
        <soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/"
                          xmlns:tem="http://tempuri.org/"
                          xmlns:vin="http://schemas.datacontract.org/2004/07/Vinculados.EntidadesNegocio"
                          xmlns:xs="http://www.w3.org/2001/XMLSchema">
           <soapenv:Header/>
           <soapenv:Body>
              <tem:MarcacionRequisitosRegistraduria>
                 <tem:tipoDocumento>1</tem:tipoDocumento>
                 <tem:numeroDocumento>1118507200</tem:numeroDocumento>
                 <tem:fechaFidelizacion>20251204</tem:fechaFidelizacion>
                 <tem:codigoApp>SWPR140</tem:codigoApp>
                 <tem:usuario>901007014</tem:usuario>
                 <tem:requisitos>
                    <vin:Requisito>
                       <vin:CampoAlfa/>
                       <vin:CampoBooleano>0</vin:CampoBooleano>
                       <vin:CampoNumerico>11001</vin:CampoNumerico>
                       <vin:DecCampoFecha1>20251204</vin:DecCampoFecha1>
                       <vin:DecCampoFecha2>0</vin:DecCampoFecha2>
                       <vin:Sigla>IDEXPE</vin:Sigla>
                    </vin:Requisito>
                    <vin:Requisito>
                       <vin:CampoAlfa>CONSULTA ANI 20250613</vin:CampoAlfa>
                       <vin:CampoBooleano>0</vin:CampoBooleano>
                       <vin:CampoNumerico>0</vin:CampoNumerico>
                       <vin:DecCampoFecha1>0</vin:DecCampoFecha1>
                       <vin:DecCampoFecha2>0</vin:DecCampoFecha2>
                       <vin:Sigla>CORNEC</vin:Sigla>
                    </vin:Requisito>
                    <vin:Requisito>
                       <vin:CampoAlfa/>
                       <vin:CampoBooleano>0</vin:CampoBooleano>
                       <vin:CampoNumerico>6</vin:CampoNumerico>
                       <vin:DecCampoFecha1>0</vin:DecCampoFecha1>
                       <vin:DecCampoFecha2>0</vin:DecCampoFecha2>
                       <vin:Sigla>ESTREG</vin:Sigla>
                    </vin:Requisito>
                    <vin:Requisito>
                       <vin:CampoAlfa/>
                       <vin:CampoBooleano>1</vin:CampoBooleano>
                       <vin:CampoNumerico>8</vin:CampoNumerico>
                       <vin:DecCampoFecha1>20260530</vin:DecCampoFecha1>
                       <vin:DecCampoFecha2>20260530</vin:DecCampoFecha2>
                       <vin:Sigla>CEDULA</vin:Sigla>
                    </vin:Requisito>
                 </tem:requisitos>
              </tem:MarcacionRequisitosRegistraduria>
           </soapenv:Body>
        </soapenv:Envelope>
        """;

    private static ServiciosClienteSoapFunction CrearFuncion()
        => new(NullLogger<ServiciosClienteSoapFunction>.Instance);

    private static HttpRequest CrearRequest(string body)
    {
        var contexto = new DefaultHttpContext();
        contexto.Request.Body =
            new MemoryStream(Encoding.UTF8.GetBytes(body));
        contexto.Request.ContentType = "text/xml; charset=utf-8";
        contexto.Request.Headers["SOAPAction"] =
            "\"http://tempuri.org/IServiciosCliente/MarcacionRequisitosRegistraduria\"";
        return contexto.Request;
    }

    [Fact]
    public void ExtraeTodasLasSiglasEnOrden()
    {
        var ok = MarcacionRequisitosRegistraduriaSoap.TryExtraerSiglas(
            RequestEjemplo,
            out var siglas,
            out var error);

        Assert.True(ok);
        Assert.Null(error);
        Assert.Equal(
            new[] { "IDEXPE", "CORNEC", "ESTREG", "CEDULA" },
            siglas);
    }

    [Fact]
    public void RespuestaEsTrueYListaCadaRequisito()
    {
        var respuesta =
            MarcacionRequisitosRegistraduriaSoap.ConstruirRespuesta(
                new[] { "IDEXPE", "CORNEC" });

        Assert.Contains("MarcacionRequisitosRegistraduriaResult", respuesta);
        Assert.Contains(">true<", respuesta);
        Assert.Contains("Requisito 'IDEXPE' fue actualizado.", respuesta);
        Assert.Contains("Requisito 'CORNEC' fue actualizado.", respuesta);
    }

    [Fact]
    public void BodyMalFormadoEsInvalido()
    {
        var ok = MarcacionRequisitosRegistraduriaSoap.TryExtraerSiglas(
            "<soapenv:Envelope><sin cerrar>",
            out _,
            out var error);

        Assert.False(ok);
        Assert.NotNull(error);
    }

    [Fact]
    public void SinOperacionEsInvalido()
    {
        var ok = MarcacionRequisitosRegistraduriaSoap.TryExtraerSiglas(
            "<Envelope><Body><OtraOperacion/></Body></Envelope>",
            out _,
            out var error);

        Assert.False(ok);
        Assert.NotNull(error);
    }

    [Fact]
    public void SinRequisitosEsInvalido()
    {
        var body = """
            <soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/"
                              xmlns:tem="http://tempuri.org/">
              <soapenv:Body>
                <tem:MarcacionRequisitosRegistraduria/>
              </soapenv:Body>
            </soapenv:Envelope>
            """;

        var ok = MarcacionRequisitosRegistraduriaSoap.TryExtraerSiglas(
            body, out _, out var error);

        Assert.False(ok);
        Assert.NotNull(error);
    }

    [Fact]
    public void ToleraNamespacesConOtrosPrefijos()
    {
        var body = """
            <a:Envelope xmlns:a="http://schemas.xmlsoap.org/soap/envelope/"
                        xmlns:b="http://tempuri.org/"
                        xmlns:c="http://schemas.datacontract.org/2004/07/Vinculados.EntidadesNegocio">
              <a:Body>
                <b:MarcacionRequisitosRegistraduria>
                  <b:requisitos>
                    <c:Requisito><c:Sigla>IDEXPE</c:Sigla></c:Requisito>
                    <c:Requisito><c:Sigla>CEDULA</c:Sigla></c:Requisito>
                  </b:requisitos>
                </b:MarcacionRequisitosRegistraduria>
              </a:Body>
            </a:Envelope>
            """;

        var ok = MarcacionRequisitosRegistraduriaSoap.TryExtraerSiglas(
            body, out var siglas, out _);

        Assert.True(ok);
        Assert.Equal(new[] { "IDEXPE", "CEDULA" }, siglas);
    }

    [Fact]
    public async Task EndpointDevuelve200YListaRequisitos()
    {
        var funcion = CrearFuncion();

        var respuesta = await funcion.MarcacionRequisitosRegistraduria(
            CrearRequest(RequestEjemplo),
            CancellationToken.None);

        var contenido = Assert.IsType<ContentResult>(respuesta);
        Assert.Equal(200, contenido.StatusCode);
        Assert.Equal("text/xml; charset=utf-8", contenido.ContentType);
        Assert.Contains(">true<", contenido.Content!);
        Assert.Contains("Requisito 'IDEXPE' fue actualizado.", contenido.Content);
        Assert.Contains("Requisito 'CEDULA' fue actualizado.", contenido.Content);
    }

    [Fact]
    public async Task EndpointDevuelve400ConFaultSiElBodyEsInvalido()
    {
        var funcion = CrearFuncion();

        var respuesta = await funcion.MarcacionRequisitosRegistraduria(
            CrearRequest("<xml roto>"),
            CancellationToken.None);

        var contenido = Assert.IsType<ContentResult>(respuesta);
        Assert.Equal(400, contenido.StatusCode);
        Assert.Contains("Fault", contenido.Content);
    }
}
