using System.Net;
using Favo.Books.Modules.Identity;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.OpenApi;
using Favo.Books.Modules.Catalog;
using Favo.Books.Modules.Reading;
using Favo.Books.Api.Infrastructure.Errors;
using Favo.Books.Modules.Profiles;
using Favo.Books.Modules.Community;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<Favo.Books.Modules.Identity.Application.Users.ReaderDirectory>();
builder.Services.AddScoped<Favo.Books.Modules.Reading.Application.PublicLibrary>();
builder.Services.AddScoped<Favo.Books.Modules.Profiles.Application.PublicShowcase>();
builder.Services.AddScoped<Favo.Books.Modules.Community.Application.PublicReaderRatings>();
builder.Services.AddScoped<Favo.Books.Api.ReaderAchievements>();
builder.Services.AddScoped<Favo.Books.Api.ReaderTaste>();
builder.Services.AddScoped<Favo.Books.Api.ReaderCompatibility>();

builder.Services.AddScoped<Favo.Books.Modules.Identity.Application.Users.ReaderNetwork>();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    // Loopback proxies are trusted by default; a remote gateway must be listed in Proxy:KnownProxies.
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor;
    options.ForwardLimit = 1;
    foreach (string proxy in builder.Configuration.GetSection("Proxy:KnownProxies").Get<string[]>() ?? [])
    {
        options.KnownProxies.Add(IPAddress.Parse(proxy));
    }
});
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
app.UseForwardedHeaders();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

}

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new
{
    service = "Favo.Books.Api",
    status = "running"
}));

app.MapHealthChecks("/health");
app.MapIdentityEndpoints();
app.MapCatalogEndpoints();
app.MapReadingEndpoints();
app.MapProfilesEndpoints();
app.MapCommunityEndpoints();
Favo.Books.Api.ReaderEndpoints.MapReaderEndpoints(app);

Favo.Books.Api.SocialEndpoints.MapSocialEndpoints(app);
app.Run();
public partial class Program;
