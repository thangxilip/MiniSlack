using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniSlack.Domain.Auth;

namespace MiniSlack.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.Property(refreshToken => refreshToken.TokenHash)
            .HasMaxLength(512)
            .IsRequired();

        builder.HasOne(refreshToken => refreshToken.User)
            .WithMany()
            .HasForeignKey(refreshToken => refreshToken.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(refreshToken => refreshToken.UserId);

        builder.HasIndex(refreshToken => refreshToken.TokenHash)
            .IsUnique();
    }
}
