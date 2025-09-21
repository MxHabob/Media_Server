using Jellyfin.Database.Implementations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jellyfin.Database.Implementations.ModelConfiguration
{
    /// <summary>
    /// Entity configuration for <see cref="SubscriptionConfiguration"/>.
    /// </summary>
    public class SubscriptionConfigurationConfiguration : IEntityTypeConfiguration<SubscriptionConfiguration>
    {
        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<SubscriptionConfiguration> builder)
        {
            builder.HasKey(e => e.Id);

            builder.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(e => e.Description)
                .HasMaxLength(500);

            builder.Property(e => e.Currency)
                .HasMaxLength(3);

            builder.Property(e => e.Price)
                .HasPrecision(18, 2);

            builder.Property(e => e.Metadata)
                .HasColumnType("TEXT");

            builder.HasIndex(e => e.IsActive);
            builder.HasIndex(e => e.SubscriptionType);
            builder.HasIndex(e => e.CreatedDate);
        }
    }
}
