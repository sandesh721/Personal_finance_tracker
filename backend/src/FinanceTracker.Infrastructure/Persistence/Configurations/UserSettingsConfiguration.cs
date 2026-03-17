using FinanceTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceTracker.Infrastructure.Persistence.Configurations;

public sealed class UserSettingsConfiguration : IEntityTypeConfiguration<UserSettings>
{
    public void Configure(EntityTypeBuilder<UserSettings> builder)
    {
        builder.ToTable("user_settings");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.PreferredCurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(x => x.DateFormat).HasMaxLength(32).IsRequired();
        builder.Property(x => x.LandingPage).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Theme).HasMaxLength(32).IsRequired();
        builder.Property(x => x.DefaultPaymentMethod).HasMaxLength(64);
        builder.Property(x => x.DefaultBudgetAlertThresholdPercent).IsRequired();

        builder.HasIndex(x => x.UserId).IsUnique();

        builder.HasOne(x => x.User)
            .WithOne(x => x.Settings)
            .HasForeignKey<UserSettings>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.DefaultAccount)
            .WithMany()
            .HasForeignKey(x => x.DefaultAccountId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}