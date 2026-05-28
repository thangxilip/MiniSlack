using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniSlack.Domain.Workspaces;

namespace MiniSlack.Infrastructure.Persistence.Configurations;

public sealed class WorkspaceInviteConfiguration : IEntityTypeConfiguration<WorkspaceInvite>
{
    public void Configure(EntityTypeBuilder<WorkspaceInvite> builder)
    {
        builder.ToTable("workspace_invites");

        builder.Property(invite => invite.Email)
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(invite => invite.NormalizedEmail)
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(invite => invite.Role)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(invite => invite.TokenHash)
            .HasMaxLength(128)
            .IsRequired();

        builder.HasOne(invite => invite.Workspace)
            .WithMany(workspace => workspace.Invites)
            .HasForeignKey(invite => invite.WorkspaceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(invite => invite.InvitedByUser)
            .WithMany()
            .HasForeignKey(invite => invite.InvitedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(invite => invite.AcceptedByUser)
            .WithMany()
            .HasForeignKey(invite => invite.AcceptedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(invite => invite.TokenHash)
            .IsUnique();

        builder.HasIndex(invite => new
        {
            invite.WorkspaceId,
            invite.NormalizedEmail
        });
    }
}
