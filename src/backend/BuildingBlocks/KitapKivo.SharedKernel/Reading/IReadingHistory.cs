namespace KitapKivo.SharedKernel.Reading;

public interface IReadingHistory
{
    Task<IReadOnlyList<Guid>> GetStartedBookIdsAsync(Guid userId, CancellationToken cancellationToken = default);
}
