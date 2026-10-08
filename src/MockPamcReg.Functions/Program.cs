using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MockPamcReg.Functions.Options;
using MockPamcReg.Functions.Storage;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;

        services.Configure<MockStorageOptions>(
            configuration.GetSection(MockStorageOptions.Seccion));

        services.AddSingleton<IRegistraduriaMockRepository>(
            _ => CrearRepositorio(configuration));
    })
    .Build();

await host.RunAsync();

return;

static IRegistraduriaMockRepository CrearRepositorio(IConfiguration configuration)
{
    var contenedor =
        configuration["Storage:Container"]
        ?? "mock-registraduria";

    var accountUri = configuration["Storage:AccountUri"];

    // Modo identidad (RBAC / Entra ID): recomendado en Azure.
    if (!string.IsNullOrWhiteSpace(accountUri))
    {
        var servicio = new BlobServiceClient(
            new Uri(accountUri),
            new DefaultAzureCredential());

        return new BlobRegistraduriaMockRepository(
            servicio.GetBlobContainerClient(contenedor));
    }

    // Fallback local (Azurite o connection string de desarrollo).
    var conexion =
        configuration["Storage:ConnectionString"]
        ?? configuration["AzureWebJobsStorage"]
        ?? "UseDevelopmentStorage=true";

    var servicioLocal = new BlobServiceClient(conexion);

    return new BlobRegistraduriaMockRepository(
        servicioLocal.GetBlobContainerClient(contenedor));
}

public partial class Program;
