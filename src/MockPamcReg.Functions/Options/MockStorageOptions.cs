namespace MockPamcReg.Functions.Options;

/// <summary>
/// Configuración de acceso al almacenamiento de respuestas mock.
/// </summary>
public sealed class MockStorageOptions
{
    public const string Seccion = "Storage";

    /// <summary>
    /// URI de la cuenta de blob (p. ej. https://mockpamcreg.blob.core.windows.net).
    /// Si se define, la lectura se hace con identidad (DefaultAzureCredential).
    /// </summary>
    public string? AccountUri { get; set; }

    /// <summary>
    /// Contenedor donde vive un JSON por documento.
    /// </summary>
    public string Container { get; set; } = "mock-registraduria";

    /// <summary>
    /// Connection string opcional (fallback local/Azurite).
    /// </summary>
    public string? ConnectionString { get; set; }
}
