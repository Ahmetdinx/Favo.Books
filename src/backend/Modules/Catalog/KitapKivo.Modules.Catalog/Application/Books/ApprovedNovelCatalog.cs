using System.Text.Json;

namespace KitapKivo.Modules.Catalog.Application.Books;

public sealed record ApprovedNovel(string WorkId, string WorkTitle, string TurkishTitle,
    string[] AuthorNames, string SourceUrl, string EvidenceNote, int Priority);

public interface IApprovedNovelCatalog
{
    IReadOnlyList<ApprovedNovel> Items { get; }
    ApprovedNovel? Find(string workId);
}

/// <summary>Trusted, source-reviewed release data; never populated from an HTTP request.</summary>
public sealed class ApprovedNovelCatalog : IApprovedNovelCatalog
{
    public static ApprovedNovelCatalog Default { get; } = new();
    public IReadOnlyList<ApprovedNovel> Items { get; }
    public ApprovedNovelCatalog()
    {
        using var stream = typeof(ApprovedNovelCatalog).Assembly.GetManifestResourceStream("novel-import-priorities.json")
            ?? throw new InvalidOperationException("Reviewed novel import list missing.");
        var items = JsonSerializer.Deserialize<ApprovedNovel[]>(stream)
            ?? throw new InvalidOperationException("Invalid reviewed import list.");
        if (items.Select(x => x.WorkId).Distinct().Count() != items.Length)
            throw new InvalidOperationException("Duplicate reviewed work IDs.");
        Items = Array.AsReadOnly(items.OrderBy(x => x.Priority).ThenBy(x => x.WorkId, StringComparer.Ordinal).ToArray());
    }
    public ApprovedNovel? Find(string workId) => Items.FirstOrDefault(x => x.WorkId == workId);
}
