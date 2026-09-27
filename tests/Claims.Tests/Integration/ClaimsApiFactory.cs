using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Claims.Infrastructure.Persistence;
using Claims.Tests.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Claims.Tests.Integration;

public sealed class ClaimsApiFactory(string databasePath) : WebApplicationFactory<Program>
{
    public const string Key = "integration-only-signing-key-not-for-deployment-at-least-64-bytes!!";
    public const string Issuer = "ClaimsManagementSystem";
    public const string Audience = "ClaimsManagementSystem.Clients";
    public static readonly DateOnly Today = new(2026, 9, 27);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.UseSetting("Database:Provider", "Sqlite");
        builder.UseSetting("ConnectionStrings:ClaimsDb", $"Data Source={databasePath};Pooling=False");
        builder.UseSetting("Jwt:SigningKey", Key);
        builder.UseSetting("Jwt:Issuer", Issuer);
        builder.UseSetting("Jwt:Audience", Audience);
        builder.UseSetting("Seed:Enabled", "false");
        builder.UseSetting("Storage:LocalRootPath", Path.Combine(AppContext.BaseDirectory, "test-documents"));
        builder.ConfigureLogging(logging => logging.ClearProviders());
    }

    public HttpClient Client(string? token = null)
    {
        var client = CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        if (token is not null) client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        return client;
    }

    public static SqliteClaimsDbContext Database(string path) => new(
        new DbContextOptionsBuilder<SqliteClaimsDbContext>().UseSqlite($"Data Source={path};Pooling=False").Options,
        new FixedClock(Today));

    public static string Token(string role = "Admin", Guid? customerId = null, string key = Key,
        string issuer = Issuer, string audience = Audience, bool expired = false, bool includeExpiration = true)
    {
        var claims = new List<Claim> { new("sub", Guid.NewGuid().ToString()), new(ClaimTypes.Role, role) };
        if (customerId is { } id) claims.Add(new("customer_id", id.ToString()));
        var token = new JwtSecurityToken(issuer, audience, claims,
            DateTime.UtcNow.AddMinutes(-10), includeExpiration
                ? expired ? DateTime.UtcNow.AddMinutes(-5) : DateTime.UtcNow.AddMinutes(5) : null,
            new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
