using Favo.Books.Modules.Catalog.Domain.Books;
using Favo.Books.Modules.Catalog.Domain.Sources;

namespace Favo.Books.Modules.Catalog.Domain.Authors;

internal sealed class Author
{
    private Author()
    {
    }

    public Author(
        string name,
        string? biography = null,
        DateOnly? birthDate = null,
        DateOnly? deathDate = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (birthDate.HasValue &&
            deathDate.HasValue &&
            deathDate.Value < birthDate.Value)
        {
            throw new ArgumentException(
                "Death date cannot be earlier than birth date.",
                nameof(deathDate));
        }

        Id = Guid.NewGuid();
        Name = name.Trim();
        Biography = NormalizeOptionalValue(biography);
        BirthDate = birthDate;
        DeathDate = deathDate;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Biography { get; private set; }

    public DateOnly? BirthDate { get; private set; }

    public DateOnly? DeathDate { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public ICollection<BookAuthor> Books { get; } = [];

    public ICollection<ExternalReference> ExternalReferences { get; } = [];

    private static string? NormalizeOptionalValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}