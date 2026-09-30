using System.Xml.Linq;

using Xunit;

namespace Favo.Books.ArchitectureTests;

public sealed class ProjectDependencyTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Theory]
    [InlineData("Identity")]
    [InlineData("Catalog")]
    [InlineData("Reading")]
    [InlineData("Community")]
    [InlineData("Profiles")]
    public void Modules_should_reference_only_SharedKernel(string moduleName)
    {
        var projectName = $"Favo.Books.Modules.{moduleName}";

        var projectPath = Path.Combine(
            RepositoryRoot,
            "src",
            "backend",
            "Modules",
            moduleName,
            projectName,
            $"{projectName}.csproj");

        var references = GetProjectReferences(projectPath);

        Assert.Equal(
            new[] { "Favo.Books.SharedKernel" },
            references);
    }

    [Fact]
    public void SharedKernel_should_not_reference_any_project()
    {
        var projectPath = Path.Combine(
            RepositoryRoot,
            "src",
            "backend",
            "BuildingBlocks",
            "Favo.Books.SharedKernel",
            "Favo.Books.SharedKernel.csproj");

        var references = GetProjectReferences(projectPath);

        Assert.Empty(references);
    }

    [Fact]
    public void Api_should_reference_all_modules()
    {
        var projectPath = Path.Combine(
            RepositoryRoot,
            "src",
            "backend",
            "Api",
            "Favo.Books.Api",
            "Favo.Books.Api.csproj");

        var expectedReferences = new[]
     {
    "Favo.Books.Modules.Catalog",
    "Favo.Books.Modules.Community",
    "Favo.Books.Modules.Identity",
    "Favo.Books.Modules.Profiles",
    "Favo.Books.Modules.Reading"
    };

        var references = GetProjectReferences(projectPath);

        Assert.Equal(expectedReferences, references);
    }

    private static string[] GetProjectReferences(string projectPath)
    {
        var document = XDocument.Load(projectPath);

        return document
            .Descendants()
            .Where(element => element.Name.LocalName == "ProjectReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(reference => !string.IsNullOrWhiteSpace(reference))
            .Select(reference =>
                Path.GetFileNameWithoutExtension(
                    reference!.Replace('\\', '/'))!)
            .OrderBy(projectName => projectName, StringComparer.Ordinal)
            .ToArray();
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var solutionPath = Path.Combine(
                directory.FullName,
                "Favo.Books.slnx");

            if (File.Exists(solutionPath))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Favo.Books.slnx dosyası bulunamadı.");
    }
}