using MockPamcReg.Functions.Storage;

namespace MockPamcReg.Functions.Tests.Fakes;

public sealed class FakeRegistraduriaMockRepository : IRegistraduriaMockRepository
{
    private readonly Dictionary<string, string?> _documentos;

    public FakeRegistraduriaMockRepository(
        Dictionary<string, string?>? documentos = null)
    {
        _documentos = documentos ?? new Dictionary<string, string?>();
    }

    public Task<string?> ObtenerDocumentoAsync(
        string numeroIdentificacion,
        CancellationToken ct = default)
    {
        _documentos.TryGetValue(numeroIdentificacion, out var contenido);
        return Task.FromResult(contenido);
    }
}
