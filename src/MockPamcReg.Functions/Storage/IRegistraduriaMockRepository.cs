namespace MockPamcReg.Functions.Storage;

/// <summary>
/// Acceso a las respuestas mock de Registraduría almacenadas como blobs.
/// </summary>
public interface IRegistraduriaMockRepository
{
    /// <summary>
    /// Devuelve el contenido JSON del documento
    /// (<c>{numeroIdentificacion}.json</c>) o <c>null</c> si no existe.
    /// </summary>
    Task<string?> ObtenerDocumentoAsync(
        string numeroIdentificacion,
        CancellationToken ct = default);
}
