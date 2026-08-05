namespace KitapKivo.Modules.Catalog.Application.Authors.Create;

public sealed record CreateAuthorCommand(
    string Name,
    string? Biography,
    DateOnly? BirthDate,
    DateOnly? DeathDate);