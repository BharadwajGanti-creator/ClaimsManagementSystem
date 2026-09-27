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
/// Verifies the schema for serving startup. Explicit migration mode applies
/// committed migrations and optionally bootstraps an administrator.
/// </summary>
public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default,
        bool applyMigrations = false)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        var context = sp.GetRequiredService<ClaimsDbContext>();
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");

        if (applyMigrations)
        {
            logger.LogInformation("Applying database migrations...");
            await context.Database.MigrateAsync(cancellationToken);
        }
        else
        {
            if ((await context.Database.GetPendingMigrationsAsync(cancellationToken)).Any())
                throw new InvalidOperationException("Database migrations are pending. Run the application with --migrate-database as an explicit deployment step before starting the API.");
        }

        if (applyMigrations) await SeedAdminAsync(sp, context, logger, cancellationToken);
    }

    private static async Task SeedAdminAsync(
        IServiceProvider sp,
        ClaimsDbContext context,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var config = sp.GetRequiredService<IConfiguration>();
        if (!config.GetValue<bool>("Seed:Enabled")) return;
        var hasher = sp.GetRequiredService<IPasswordHasher>();
        var clock = sp.GetRequiredService<IDateTimeProvider>();

        var adminEmail = config["Seed:AdminEmail"]?.Trim().ToLowerInvariant();
        var adminPassword = config["Seed:AdminPassword"];
        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword) || adminPassword.Length < 12)
            throw new InvalidOperationException("Seeding requires an explicit admin email and a password of at least 12 characters.");

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
