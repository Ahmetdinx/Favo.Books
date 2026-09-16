namespace KitapKivo.Modules.Community.Domain.Ratings;

/// <summary>Seven required integer scores on the same 1-10 scale.</summary>
internal sealed record RatingScores
{
    public RatingScores(int theme, int language, int characters, int structure,
        int pace, int impact, int experience)
    {
        Theme = Validate(theme, nameof(theme));
        Language = Validate(language, nameof(language));
        Characters = Validate(characters, nameof(characters));
        Structure = Validate(structure, nameof(structure));
        Pace = Validate(pace, nameof(pace));
        Impact = Validate(impact, nameof(impact));
        Experience = Validate(experience, nameof(experience));
    }

    public int Theme { get; }
    public int Language { get; }
    public int Characters { get; }
    public int Structure { get; }
    public int Pace { get; }
    public int Impact { get; }
    public int Experience { get; }

    public decimal OverallScore =>
        (Theme * RatingPolicy.ThemeWeight +
         Language * RatingPolicy.LanguageWeight +
         Characters * RatingPolicy.CharactersWeight +
         Structure * RatingPolicy.StructureWeight +
         Pace * RatingPolicy.PaceWeight +
         Impact * RatingPolicy.ImpactWeight +
         Experience * RatingPolicy.ExperienceWeight) / 100m;

    private static int Validate(int score, string name)
    {
        if (score is < RatingPolicy.MinimumScore or > RatingPolicy.MaximumScore)
            throw new ArgumentOutOfRangeException(name, "Her kriter 1 ile 10 arasında olmalıdır.");
        return score;
    }
}
