using Azure;
using Azure.Storage.Blobs;

namespace MockPamcReg.Functions.Storage;

/// <summary>
/// Implementación sobre Azure Blob Storage. Un JSON por documento:
/// <c>{contenedor}/{numeroIdentificacion}.json</c>.
/// </summary>
public sealed class BlobRegistraduriaMockRepository : IRegistraduriaMockRepository
{
    private readonly BlobContainerClient _contenedor;

    public BlobRegistraduriaMockRepository(BlobContainerClient contenedor)
    {
        _contenedor = contenedor;
    }

    public async Task<string?> ObtenerDocumentoAsync(
        string numeroIdentificacion,
        CancellationToken ct = default)
    {
        var nombreBlob = $"{numeroIdentificacion}.json";
        var blob = _contenedor.GetBlobClient(nombreBlob);

        try
        {
            var descarga = await blob.DownloadContentAsync(ct);
            return descarga.Value.Content.ToString();
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }
}
