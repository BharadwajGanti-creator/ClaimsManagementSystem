using System.Text;
using Claims.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Claims.Shared.Constants;

namespace Claims.API.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
                  ?? throw new InvalidOperationException("Missing 'Jwt' configuration section.");
        if (string.IsNullOrWhiteSpace(jwt.Issuer) || string.IsNullOrWhiteSpace(jwt.Audience)
            || string.IsNullOrWhiteSpace(jwt.SigningKey) || Encoding.UTF8.GetByteCount(jwt.SigningKey) < 32)
            throw new InvalidOperationException("JWT issuer, audience and a signing key of at least 32 bytes are required.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
                options.TokenValidationParameters.ValidAlgorithms = [SecurityAlgorithms.HmacSha256];
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        var user = context.Principal!;
                        var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
                        if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) || userId == Guid.Empty
                            || roles.Length != 1 || roles[0] is not (Roles.Admin or Roles.Adjuster or Roles.Claimant)
                            || (roles[0] == Roles.Claimant &&
                                (!Guid.TryParse(user.FindFirstValue("customer_id"), out var customerId) || customerId == Guid.Empty)))
                            context.Fail("A valid user, role and claimant customer identity are required.");
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();
        return services;
    }
}
