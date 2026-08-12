namespace KitapKivo.Modules.Catalog.Domain.Categories;

internal sealed class Category
{
    private Category()
    {
    }

    public Category(
        string name,
        string slug)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);

        string normalizedSlug =
            slug.Trim().ToLowerInvariant();

        bool containsInvalidCharacter =
            normalizedSlug.Any(character =>
                !char.IsAsciiLetterOrDigit(character) &&
                character != '-');

        if (containsInvalidCharacter ||
            normalizedSlug.StartsWith('-') ||
            normalizedSlug.EndsWith('-') ||
            normalizedSlug.Contains(
                "--",
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Category slug can contain only lowercase letters, numbers and single hyphens.",
                nameof(slug));
        }

        Id = Guid.NewGuid();
        Name = name.Trim();
        Slug = normalizedSlug;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } =
        string.Empty;

    public string Slug { get; private set; } =
        string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public ICollection<BookCategory> Books { get; } = [];
}