using Claims.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Claims.Infrastructure.Persistence.Configurations;

public sealed class ClaimConfiguration : IEntityTypeConfiguration<Claim>
{
    public void Configure(EntityTypeBuilder<Claim> builder)
    {
        builder.ToTable("Claims");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.ClaimNumber).HasMaxLength(40).IsRequired();
        builder.HasIndex(c => c.ClaimNumber).IsUnique();

        builder.Property(c => c.Type).HasConversion<string>().HasMaxLength(30);
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(30);

        builder.Property(c => c.Description).HasMaxLength(2000).IsRequired();
        builder.Property(c => c.ClaimedAmount).HasPrecision(18, 2);
        builder.Property(c => c.ApprovedAmount).HasPrecision(18, 2);
        builder.Property(c => c.DecisionNotes).HasMaxLength(2000);

        builder.HasIndex(c => c.Status);

        builder.HasMany(c => c.Documents)
            .WithOne(d => d.Claim!)
            .HasForeignKey(d => d.ClaimId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.StatusHistory)
            .WithOne(h => h.Claim!)
            .HasForeignKey(h => h.ClaimId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.Payout)
            .WithOne(p => p.Claim!)
            .HasForeignKey<Payout>(p => p.ClaimId)
            .OnDelete(DeleteBehavior.Cascade);

        // Navigation collections use backing fields populated by EF.
        builder.Navigation(c => c.Documents).UsePropertyAccessMode(PropertyAccessMode.Property);
        builder.Navigation(c => c.StatusHistory).UsePropertyAccessMode(PropertyAccessMode.Property);
    }
}
