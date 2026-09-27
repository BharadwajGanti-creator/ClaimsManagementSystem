using Claims.Application.Abstractions;
using Claims.Application.Abstractions.Identity;
using Claims.Application.Abstractions.Persistence;
using Claims.Application.Features.Auth;
using Claims.Application.Features.Claims;
using Claims.Application.Features.Customers;
using Claims.Application.Features.Payouts;
using Claims.Application.Features.Policies;
using Claims.Infrastructure.Identity;
using Claims.Infrastructure.Persistence;
using Claims.Infrastructure.Persistence.Repositories;
using Claims.Infrastructure.Storage;
using Claims.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Claims.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Wires up persistence, identity, storage and the application use-case
    /// services. This is the single composition entry point for the backend.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // --- Persistence ---
        // Provider is selectable so the same image runs against SQL Server
        // (local/production) or an embedded SQLite database for development/demo.
        var provider = configuration["Database:Provider"] ?? "SqlServer";
        var connectionString = configuration.GetConnectionString("ClaimsDb");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("ConnectionStrings:ClaimsDb is required.");
        if (provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            services.AddDbContext<SqliteClaimsDbContext>(options => options.UseSqlite(connectionString));
            services.AddScoped<ClaimsDbContext>(sp => sp.GetRequiredService<SqliteClaimsDbContext>());
        }
        else if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            services.AddDbContext<SqlServerClaimsDbContext>(options =>
                options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));
            services.AddScoped<ClaimsDbContext>(sp => sp.GetRequiredService<SqlServerClaimsDbContext>());
        }
        else throw new InvalidOperationException("Database:Provider must be SqlServer or Sqlite.");

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ClaimsDbContext>());

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IPolicyRepository, PolicyRepository>();
        services.AddScoped<IClaimRepository, ClaimRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        // --- Identity / security ---
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddScoped<IPasswordHasher, PasswordHasherAdapter>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        // --- Cross-cutting ---
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddSingleton<IFileStorage, LocalFileStorage>();

        // --- Application use-case services (Application has no DI dependency itself) ---
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IPolicyService, PolicyService>();
        services.AddScoped<IClaimService, ClaimService>();
        services.AddScoped<IPayoutService, PayoutService>();

        return services;
    }
}
