using System.Text;
using System.Text.RegularExpressions;

namespace KitapKivo.Modules.Catalog.Domain.Books;

public enum NovelEvidenceStatus { Unknown, NovelCandidate, NonNovelCandidate, Conflict }

public sealed record NovelEvidenceResult(NovelEvidenceStatus Status, string[] NovelSignals,
    string[] NonNovelSignals, string RuleVersion);

/// <summary>Conservative source-metadata triage, never an automatic publication decision.</summary>
public static class NovelEvidenceClassifier
{
    public const string Version = "subject-form-v1";

    private static readonly HashSet<string> NovelForms = new(StringComparer.Ordinal)
    {
        "novel", "novels", "roman", "romanlar", "türk romanı", "turk romani",
        "turkish novels", "english novels", "french novels", "russian novels",
        "historical novel", "historical novels", "tarihi roman", "tarihî roman",
        "tarihsel roman", "psikolojik roman", "psychological novel", "psychological novels"
    };

    private static readonly HashSet<string> OtherForms = new(StringComparer.Ordinal)
    {
        "nonfiction", "non-fiction", "non fiction", "kurgu dışı", "kurgu disi",
        "short stories", "short story", "öykü", "öyküler", "hikayeler", "hikâyeler",
        "poetry", "poems", "şiir", "şiirler", "plays", "drama", "essays", "denemeler",
        "biography", "autobiography", "memoirs", "biyografi", "otobiyografi",
        "self-help", "self help", "kişisel gelişim", "textbooks", "ders kitapları",
        "graphic novels", "comics", "novella", "novellas"
    };

    public static NovelEvidenceResult Classify(IEnumerable<string> subjects)
    {
        ArgumentNullException.ThrowIfNull(subjects);
        var distinct = subjects.Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim()).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        var novels = distinct.Where(s => NovelForms.Contains(Normalize(s))).ToArray();
        var others = distinct.Where(s => OtherForms.Contains(Normalize(s))).ToArray();
        var status = (novels.Length > 0, others.Length > 0) switch
        {
            (true, true) => NovelEvidenceStatus.Conflict,
            (true, false) => NovelEvidenceStatus.NovelCandidate,
            (false, true) => NovelEvidenceStatus.NonNovelCandidate,
            _ => NovelEvidenceStatus.Unknown
        };
        return new(status, novels, others, Version);
    }

    private static string Normalize(string value) => Regex.Replace(
        value.Normalize(NormalizationForm.FormC).Replace('İ', 'i').ToLowerInvariant(), @"\s+", " ").Trim();
}
