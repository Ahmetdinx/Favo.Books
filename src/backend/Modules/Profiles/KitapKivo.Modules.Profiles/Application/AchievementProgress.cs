namespace KitapKivo.Modules.Profiles.Application;

public sealed record AchievementBadge(int Threshold, string Title);
public sealed record AchievementTrack(string Kind, string Label, int Count, AchievementBadge? Current,
    AchievementBadge? Next, int Remaining, int ProgressPercent, AchievementBadge[] Badges);

public static class AchievementProgress
{
    public static AchievementTrack Reading(int count) => Build("reading", "Okuma", count,
        [new(1, "İlk Sayfa"), new(10, "Kitap Dostu"), new(100, "Kitap Kurdu"), new(1000, "Yaşayan Kütüphane")]);

    public static AchievementTrack Evaluation(int count) => Build("evaluation", "Değerlendirme", count,
        [new(1, "İlk Bakış"), new(10, "Dikkatli Okur"), new(100, "Roman Kâşifi"), new(1000, "Usta Değerlendirici")]);

    private static AchievementTrack Build(string kind, string label, int count, AchievementBadge[] badges)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        var current = badges.LastOrDefault(b => b.Threshold <= count);
        var next = badges.FirstOrDefault(b => b.Threshold > count);
        var floor = current?.Threshold ?? 0;
        var percent = next is null ? 100 : (int)((long)(count - floor) * 100 / (next.Threshold - floor));
        return new(kind, label, count, current, next, next is null ? 0 : next.Threshold - count, percent, badges);
    }
}
