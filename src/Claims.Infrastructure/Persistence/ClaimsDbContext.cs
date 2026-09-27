using System.Reflection;
using Claims.Application.Abstractions;
using Claims.Application.Abstractions.Persistence;
using Claims.Domain.Common;
using Claims.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Claims.Infrastructure.Persistence;

public class ClaimsDbContext : DbContext, IUnitOfWork
{
    private readonly IDateTimeProvider _clock;

    public ClaimsDbContext(DbContextOptions<ClaimsDbContext> options, IDateTimeProvider clock)
        : this((DbContextOptions)options, clock) { }

    protected ClaimsDbContext(DbContextOptions options, IDateTimeProvider clock)
        : base(options)
    {
        _clock = clock;
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Policy> Policies => Set<Policy>();
    public DbSet<Coverage> Coverages => Set<Coverage>();
    public DbSet<Claim> Claims => Set<Claim>();
    public DbSet<ClaimDocument> ClaimDocuments => Set<ClaimDocument>();
    public DbSet<ClaimStatusHistory> ClaimStatusHistory => Set<ClaimStatusHistory>();
    public DbSet<Payout> Payouts => Set<Payout>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        // The domain assigns every entity ID. New children of tracked aggregates
        // must be inserted even though their GUID is already populated.
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
            if (typeof(BaseEntity).IsAssignableFrom(entity.ClrType))
                modelBuilder.Entity(entity.ClrType).Property(nameof(BaseEntity.Id)).ValueGeneratedNever();
        if (Database.IsSqlite())
        {
            // SQLite cannot order DateTimeOffset values. Store UTC ticks so pagination
            // preserves chronological ordering; both provider models have separate migrations.
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
                foreach (var property in entity.GetProperties())
                {
                    if (property.ClrType == typeof(DateTimeOffset))
                        property.SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTimeOffset, long>(
                            value => value.UtcTicks, value => new DateTimeOffset(value, TimeSpan.Zero)));
                    else if (property.ClrType == typeof(DateTimeOffset?))
                        property.SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTimeOffset?, long?>(
                            value => value.HasValue ? value.Value.UtcTicks : null,
                            value => value.HasValue ? new DateTimeOffset(value.Value, TimeSpan.Zero) : null));
                }
        }
        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Backstop for audit timestamps in case a service forgets to set them.
    /// Services normally stamp CreatedBy/UpdatedBy with the acting user.
    /// </summary>
    private void StampAuditFields()
    {
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added when entry.Entity.CreatedAtUtc == default:
                    entry.Entity.CreatedAtUtc = _clock.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAtUtc ??= _clock.UtcNow;
                    break;
            }
        }
    }
}
