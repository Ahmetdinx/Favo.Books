using KitapKivo.Modules.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services.AddIdentityModule();

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    service = "KitapKivo.Api",
    status = "running"
}));

app.MapHealthChecks("/health");

app.Run();