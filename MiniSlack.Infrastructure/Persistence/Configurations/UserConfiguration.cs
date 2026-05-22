using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniSlack.Domain.Users;

namespace MiniSlack.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.Property(user => user.GoogleId)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(user => user.Email)
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(user => user.DisplayName)
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(user => user.AvatarUrl)
            .HasMaxLength(2048);

        builder.Property(user => user.Status)
            .HasMaxLength(160);

        builder.HasIndex(user => user.GoogleId)
            .IsUnique();

        builder.HasIndex(user => user.Email)
            .IsUnique();
    }
}
