namespace Favo.Books.Modules.Community.Domain.Ratings;

internal static class RatingPolicy
{
    public const int MinimumScore = 1;
    public const int MaximumScore = 10;
    public const int ThemeWeight = 15;
    public const int LanguageWeight = 15;
    public const int CharactersWeight = 15;
    public const int StructureWeight = 15;
    public const int PaceWeight = 10;
    public const int ImpactWeight = 15;
    public const int ExperienceWeight = 15;

    public static IReadOnlyList<RatingCriterion> Criteria { get; } =
        Array.AsReadOnly(new[]
        {
            new RatingCriterion("theme", "Konu / Fikir", ThemeWeight,
                "Kitabın temel konusu ne kadar ilgi çekici? Özgün mü, potansiyeli iyi kullanılmış mı?"),
            new RatingCriterion("language", "Anlatım ve Dil", LanguageWeight,
                "Yazarın dili akıcı mı? Cümleler, betimlemeler ve diyaloglar başarılı mı? Okurken gereksiz zorlanıyor musunuz?"),
            new RatingCriterion("characters", "Karakterler", CharactersWeight,
                "Karakterler inandırıcı ve çok boyutlu mu? Gelişim gösteriyorlar mı? Davranışlarının motivasyonu anlaşılır mı?"),
            new RatingCriterion("structure", "Kurgu ve Yapı", StructureWeight,
                "Olaylar veya fikirler iyi organize edilmiş mi? Bölümler birbirini doğal biçimde takip ediyor mu? Tutarsızlıklar var mı?"),
            new RatingCriterion("pace", "Tempo / Sürükleyicilik", PaceWeight,
                "Kitap sizi okumaya devam etmeye teşvik ediyor mu? Gereksiz uzayan veya aşırı hızlı geçilen bölümler var mı?"),
            new RatingCriterion("impact", "Duygusal / Düşünsel Etki", ImpactWeight,
                "Kitap size bir şey hissettirdi veya düşündürdü mü? Bitirdikten birkaç gün sonra hâlâ aklınızda kalan şeyler var mı?"),
            new RatingCriterion("experience", "Genel Okuma Deneyimi", ExperienceWeight,
                "Diğer kriterlerden bağımsız olarak, kitabı okumaktan ne kadar keyif aldınız? Tekrar okur veya başkasına önerir miydiniz?")
        });
}

internal sealed record RatingCriterion(string Key, string Label, int Weight, string Description);
