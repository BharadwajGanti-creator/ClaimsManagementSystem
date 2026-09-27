using Claims.API.Extensions;
using Claims.API.Middleware;
using Claims.API.Security;
using Claims.Application.Abstractions.Identity;
using Claims.Infrastructure;
using Claims.Infrastructure.Persistence;
using Serilog;
using Claims.API.Health;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var migrateOnly = args.Contains("--migrate-database", StringComparer.Ordinal);
var builder = WebApplication.CreateBuilder(args.Where(arg => arg != "--migrate-database").ToArray());

// --- Structured logging ---
builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration)
          .Enrich.FromLogContext()
          .WriteTo.Console());

// --- Services ---
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddSwaggerWithJwt();
builder.Services.AddHealthChecks().AddCheck<DatabaseReadinessCheck>("database", tags: ["ready"]);

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()));

var app = builder.Build();

// --- Pipeline ---
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Swagger is exposed in all environments so the deployed API is self-documenting.
app.UseSwagger();
app.UseSwaggerUI();

app.UseSerilogRequestLogging();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" })).AllowAnonymous();
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
}).AllowAnonymous();
app.MapGet("/", () => Results.Redirect("/swagger")).AllowAnonymous();

// Serving processes only verify the schema. Migration mode applies it and exits.
await DbInitializer.InitializeAsync(app.Services, applyMigrations: migrateOnly);
if (migrateOnly) return;

app.Run();

// Exposed so WebApplicationFactory-based integration tests can reference the entry point.
public partial class Program { }
