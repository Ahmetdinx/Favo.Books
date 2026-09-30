using Favo.Books.Modules.Catalog.Domain.Editions;
using Favo.Books.Modules.Catalog.Domain.Sources;

namespace Favo.Books.Modules.Catalog.Domain.Publishers;

internal sealed class Publisher
{
    private Publisher()
    {
    }

    public Publisher(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Id = Guid.NewGuid();
        Name = name.Trim();
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public ICollection<Edition> Editions { get; } = [];

    public ICollection<ExternalReference> ExternalReferences { get; } = [];
}