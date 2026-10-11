using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using SupportDesk.Application;
using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Triage;
using SupportDesk.Infrastructure;
using SupportDesk.Presentation.Auth;
using SupportDesk.Presentation.Extensions;
using SupportDesk.Presentation.Filters;
using SupportDesk.Presentation.Health;
using SupportDesk.Presentation.Middleware;

var builder = WebApplication.CreateBuilder(args);

const string FrontendCorsPolicy = "frontend";

builder.Services
    .AddControllers(options => options.Filters.Add<ValidationFilter>())
    .AddJsonOptions(options =>
    {
        // Enums travel as their names, both ways, so the API reads well from the browser.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddCors(options => options.AddPolicy(
    FrontendCorsPolicy,
    policy => policy
        .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
        .AllowAnyHeader()
        .AllowAnyMethod()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// The SLA rules come from configuration, and a bad configuration stops the app at startup.
builder.Services.AddSingleton(
    (builder.Configuration.GetSection(SlaOptions.SectionName).Get<SlaOptions>()
        ?? throw new InvalidOperationException($"Configuration section '{SlaOptions.SectionName}' is missing."))
    .EnsureValid());

// Tokens: the signing key comes from configuration, never from code.
var jwt = JwtOptions.Create(builder.Configuration, builder.Environment);
builder.Services.AddSingleton(jwt);
builder.Services.AddSingleton<ITokenIssuer, JwtTokenIssuer>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Keep the claim names exactly as issued ("sub", "name", "email").
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = jwt.SigningKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = AgentClaims.Name
        };
    });

// Every endpoint needs a signed-in agent unless it opts out with [AllowAnonymous]. Decided
// here, once, so a new controller is closed by default instead of open by accident.
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

// GET /health: is the database reachable?
builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

if (jwt.KeyWasGenerated)
{
    app.Logger.LogWarning(
        "Jwt:Key is not configured, so a temporary signing key is used for this run. "
        + "Signed-in users have to sign in again whenever the API restarts.");
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Keeps a developer machine one command away from a working database.
    await app.MigrateAndSeedAsync();
}

app.UseCors(FrontendCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Open without a token, so a container orchestrator or a person can check on the API.
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();
