using Favo.Books.Modules.Catalog.Application
    .Abstractions.ExternalBooks;
using Favo.Books.Modules.Catalog.Infrastructure
    .ExternalBooks.OpenLibrary;

namespace Favo.Books.Modules.Catalog.UnitTests
    .Infrastructure.ExternalBooks.OpenLibrary;

public sealed class OpenLibraryAuthorDeduplicatorTests
{
    [Fact]
    public void Deduplicate_ShouldKeepCanonicalAuthor_WhenDuplicateComesFirst()
    {
        // Arrange
        OpenLibraryResolvedAuthor[] authors =
        [
            new(
                "/authors/OL7388009A",
                "Френк Герберт",
                []),
            new(
                "/authors/OL79034A",
                "Frank Herbert",
                [
                    "Herbert, Frank",
                    "Фрэнк Герберт",
                    "frank herbert"
                ])
        ];

        // Act
        IReadOnlyList<ExternalBookAuthor> result =
            OpenLibraryAuthorDeduplicator
                .Deduplicate(authors);

        // Assert
        ExternalBookAuthor author =
            Assert.Single(result);

        Assert.Equal(
            "/authors/OL79034A",
            author.ExternalId);

        Assert.Equal(
            "Frank Herbert",
            author.Name);
    }

    [Fact]
    public void Deduplicate_ShouldRemoveExactAlternativeNameDuplicate()
    {
        // Arrange
        OpenLibraryResolvedAuthor[] authors =
        [
            new(
                "/authors/OL79034A",
                "Frank Herbert",
                [
                    "Herbert, Frank"
                ]),
            new(
                "/authors/OL99999A",
                "Herbert, Frank",
                [])
        ];

        // Act
        IReadOnlyList<ExternalBookAuthor> result =
            OpenLibraryAuthorDeduplicator
                .Deduplicate(authors);

        // Assert
        ExternalBookAuthor author =
            Assert.Single(result);

        Assert.Equal(
            "/authors/OL79034A",
            author.ExternalId);

        Assert.Equal(
            "Frank Herbert",
            author.Name);
    }

    [Fact]
    public void Deduplicate_ShouldKeepDifferentAuthors()
    {
        // Arrange
        OpenLibraryResolvedAuthor[] authors =
        [
            new(
                "/authors/OL79034A",
                "Frank Herbert",
                [
                    "Herbert, Frank"
                ]),
            new(
                "/authors/OL12345A",
                "Bill Ransom",
                [
                    "Ransom, Bill"
                ])
        ];

        // Act
        IReadOnlyList<ExternalBookAuthor> result =
            OpenLibraryAuthorDeduplicator
                .Deduplicate(authors);

        // Assert
        Assert.Collection(
            result,
            author =>
            {
                Assert.Equal(
                    "/authors/OL79034A",
                    author.ExternalId);

                Assert.Equal(
                    "Frank Herbert",
                    author.Name);
            },
            author =>
            {
                Assert.Equal(
                    "/authors/OL12345A",
                    author.ExternalId);

                Assert.Equal(
                    "Bill Ransom",
                    author.Name);
            });
    }
}