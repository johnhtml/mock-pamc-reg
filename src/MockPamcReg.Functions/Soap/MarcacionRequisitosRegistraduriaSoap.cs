using System.Xml;
using System.Xml.Linq;

namespace MockPamcReg.Functions.Soap;

/// <summary>
/// Parser y builder para la operación SOAP
/// <c>MarcacionRequisitosRegistraduria</c> de Gestión Clientes
/// (<c>http://gclientespru.compensar.com/ServiciosCliente.svc</c>).
///
/// El mock extrae TODAS las siglas de los <c>Requisito</c> recibidos y simula
/// que todos fueron actualizados.
/// </summary>
public static class MarcacionRequisitosRegistraduriaSoap
{
    private static readonly XNamespace SoapEnv =
        "http://schemas.xmlsoap.org/soap/envelope/";

    private static readonly XNamespace TempUri =
        "http://tempuri.org/";

    private const string NombreOperacion =
        "MarcacionRequisitosRegistraduria";

    private const string NombreRespuesta =
        "MarcacionRequisitosRegistraduriaResponse";

    /// <summary>
    /// Extrae las siglas de todos los <c>Requisito</c> del envelope.
    /// Devuelve <c>false</c> (con <paramref name="error"/>) cuando el body es
    /// inválido: XML mal formado, sin la operación o sin requisitos.
    /// </summary>
    public static bool TryExtraerSiglas(
        string? body,
        out List<string> siglas,
        out string? error)
    {
        siglas = new List<string>();
        error = null;

        if (string.IsNullOrWhiteSpace(body))
        {
            error = "El body SOAP está vacío.";
            return false;
        }

        XDocument documento;

        try
        {
            documento = XDocument.Parse(body, LoadOptions.None);
        }
        catch (XmlException ex)
        {
            error = $"El body SOAP no es XML válido: {ex.Message}";
            return false;
        }

        var operacion = documento
            .Descendants()
            .FirstOrDefault(e =>
                e.Name.LocalName == NombreOperacion);

        if (operacion is null)
        {
            error =
                $"El body SOAP no contiene la operación {NombreOperacion}.";
            return false;
        }

        foreach (var requisito in operacion
                     .Descendants()
                     .Where(e => e.Name.LocalName == "Requisito"))
        {
            var sigla = requisito
                .Elements()
                .FirstOrDefault(e => e.Name.LocalName == "Sigla")
                ?.Value
                .Trim();

            if (!string.IsNullOrWhiteSpace(sigla))
            {
                siglas.Add(sigla!);
            }
        }

        if (siglas.Count == 0)
        {
            error =
                $"La operación {NombreOperacion} no contiene requisitos.";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Construye la respuesta SOAP exitosa. El resultado siempre es
    /// <c>true</c> y el mensaje lista una línea por sigla actualizada.
    /// </summary>
    public static string ConstruirRespuesta(IEnumerable<string> siglas)
    {
        var mensaje = string.Join(
            "\n",
            siglas.Select(s => $"Requisito '{s}' fue actualizado."));

        var operacion = new XElement(
            TempUri + NombreRespuesta,
            new XElement(
                TempUri + "MarcacionRequisitosRegistraduriaResult",
                "true"),
            new XElement(TempUri + "mensaje", mensaje));

        return Serializar(EnvolverBody(operacion));
    }

    /// <summary>
    /// Construye un SOAP Fault (body para respuestas de body inválido).
    /// </summary>
    public static string ConstruirFault(string detalle)
    {
        var fault = new XElement(
            SoapEnv + "Fault",
            new XElement(SoapEnv + "faultcode", "soap:Client"),
            new XElement(SoapEnv + "faultstring", detalle));

        return Serializar(EnvolverBody(fault));
    }

    private static XElement EnvolverBody(XElement contenido)
    {
        return new XElement(
            SoapEnv + "Envelope",
            new XAttribute(
                XNamespace.Xmlns + "s",
                SoapEnv.NamespaceName),
            new XElement(SoapEnv + "Body", contenido));
    }

    private static string Serializar(XElement envelope)
    {
        var documento = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            envelope);

        return documento.ToString(SaveOptions.DisableFormatting);
    }
}
