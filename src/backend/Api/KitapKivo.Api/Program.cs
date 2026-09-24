using KitapKivo.Modules.Identity;
using Microsoft.OpenApi;
using KitapKivo.Modules.Catalog;
using KitapKivo.Modules.Reading;
using KitapKivo.Api.Infrastructure.Errors;
using KitapKivo.Modules.Profiles;
using KitapKivo.Modules.Community;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<KitapKivo.Modules.Identity.Application.Users.ReaderDirectory>();
builder.Services.AddScoped<KitapKivo.Modules.Reading.Application.PublicLibrary>();
builder.Services.AddScoped<KitapKivo.Modules.Profiles.Application.PublicShowcase>();
builder.Services.AddScoped<KitapKivo.Modules.Community.Application.PublicReaderRatings>();

builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddProblemDetails();
builder.Services.AddProfilesModule(
    builder.Configuration);
builder.Services.AddExceptionHandler<
    GlobalExceptionHandler>();
builder.Services.AddSwaggerGen(options =>
{
    const string schemeId = "Bearer";

    options.AddSecurityDefinition(schemeId, new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "JWT access token değerini girin."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference(schemeId, document)] = []
    });
});

builder.Services.AddIdentityModule(builder.Configuration);
builder.Services.AddCatalogModule(builder.Configuration);
builder.Services.AddReadingModule(builder.Configuration);
builder.Services.AddCommunityModule(builder.Configuration);


var app = builder.Build();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new
{
    service = "KitapKivo.Api",
    status = "running"
}));

app.MapHealthChecks("/health");
app.MapIdentityEndpoints();
app.MapCatalogEndpoints();
app.MapReadingEndpoints();
app.MapProfilesEndpoints();
app.MapCommunityEndpoints();
KitapKivo.Api.ReaderEndpoints.MapReaderEndpoints(app);

app.Run();
public partial class Program;
