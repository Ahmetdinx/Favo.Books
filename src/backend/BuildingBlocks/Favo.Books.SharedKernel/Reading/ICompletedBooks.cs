namespace Favo.Books.SharedKernel.Reading;

/// <summary>Caller-scoped completed books, newest status change first. Reading owns this query.</summary>
public interface ICompletedBooks
{
    Task<IReadOnlyList<Guid>> GetBookIdsAsync(Guid userId, int offset, int limit,
        CancellationToken cancellationToken = default);
}
