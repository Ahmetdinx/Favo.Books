namespace KitapKivo.Modules.Catalog.Presentation.Authors.Create;

public sealed record CreateAuthorRequest(
    string? Name,
    string? Biography,
    DateOnly? BirthDate,
    DateOnly? DeathDate);