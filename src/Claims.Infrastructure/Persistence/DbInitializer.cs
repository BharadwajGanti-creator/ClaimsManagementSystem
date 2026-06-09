using Claims.Application.Abstractions;
using Claims.Application.Abstractions.Identity;
using Claims.Domain.Entities;
using Claims.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Claims.Infrastructure.Persistence;

/// <summary>
/// Applies migrations (or creates the schema) and seeds a default admin account
/// on startup. Idempotent: safe to run on every launch.
/// </summary>
public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        var context = sp.GetRequiredService<ClaimsDbContext>();
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");

        if (context.Database.GetMigrations().Any())
        {
            logger.LogInformation("Applying database migrations...");
            await context.Database.MigrateAsync(cancellationToken);
        }
        else
        {
            // No migrations checked in yet — create the schema directly so the app runs.
            logger.LogInformation("No migrations found; ensuring database is created.");
            await context.Database.EnsureCreatedAsync(cancellationToken);
        }

        await SeedAdminAsync(sp, context, logger, cancellationToken);
    }

    private static async Task SeedAdminAsync(
        IServiceProvider sp,
        ClaimsDbContext context,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var config = sp.GetRequiredService<IConfiguration>();
        var hasher = sp.GetRequiredService<IPasswordHasher>();
        var clock = sp.GetRequiredService<IDateTimeProvider>();

        var adminEmail = (config["Seed:AdminEmail"] ?? "admin@claims.local").ToLowerInvariant();
        var adminPassword = config["Seed:AdminPassword"] ?? "Admin#12345";

        if (await context.Users.AnyAsync(u => u.Email == adminEmail, cancellationToken))
            return;

        context.Users.Add(new User
        {
            Email = adminEmail,
            PasswordHash = hasher.Hash(adminPassword),
            FirstName = "System",
            LastName = "Administrator",
            Role = UserRole.Admin,
            CreatedAtUtc = clock.UtcNow,
            CreatedBy = "seed"
        });

        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded default admin account '{Email}'.", adminEmail);
    }
}
