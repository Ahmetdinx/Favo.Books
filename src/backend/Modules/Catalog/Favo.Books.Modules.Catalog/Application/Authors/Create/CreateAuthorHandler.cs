using Favo.Books.Modules.Catalog.Domain.Authors;
using Favo.Books.Modules.Catalog.Persistence;

namespace Favo.Books.Modules.Catalog.Application.Authors.Create;

public sealed class CreateAuthorHandler
{
    private readonly CatalogDbContext _dbContext;

    public CreateAuthorHandler(
        CatalogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CreateAuthorResult> HandleAsync(
        CreateAuthorCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        Author author = new(
            command.Name,
            command.Biography,
            command.BirthDate,
            command.DeathDate);

        _dbContext.Authors.Add(author);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return new CreateAuthorResult(author.Id);
    }
}