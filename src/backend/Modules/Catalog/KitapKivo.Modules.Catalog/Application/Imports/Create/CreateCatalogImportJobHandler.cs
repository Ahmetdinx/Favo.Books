using KitapKivo.Modules.Catalog.Domain.Imports;
using KitapKivo.Modules.Catalog.Persistence;

namespace KitapKivo.Modules.Catalog.Application
    .Imports.Create;

public sealed class CreateCatalogImportJobHandler
{
    private const string SupportedProvider =
        "OpenLibrary";

    private readonly CatalogDbContext _dbContext;

    public CreateCatalogImportJobHandler(
        CatalogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CreateCatalogImportJobResult>
        HandleAsync(
            CreateCatalogImportJobCommand command,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            command.Provider);

        if (!string.Equals(
                command.Provider.Trim(),
                SupportedProvider,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Only the OpenLibrary provider is supported.",
                nameof(command));
        }

        CatalogImportJob job = new(
            SupportedProvider,
            command.Query,
            command.StartPage,
            command.PageSize,
            command.PageCount);

        _dbContext.ImportJobs.Add(job);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return new CreateCatalogImportJobResult(
            job.Id,
            job.Status.ToString(),
            job.CreatedAtUtc);
    }
}