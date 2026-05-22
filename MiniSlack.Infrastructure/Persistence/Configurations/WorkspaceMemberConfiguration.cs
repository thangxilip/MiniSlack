using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniSlack.Domain.Workspaces;

namespace MiniSlack.Infrastructure.Persistence.Configurations;

public sealed class WorkspaceMemberConfiguration : IEntityTypeConfiguration<WorkspaceMember>
{
    public void Configure(EntityTypeBuilder<WorkspaceMember> builder)
    {
        builder.ToTable("workspace_members");

        builder.Property(workspaceMember => workspaceMember.Role)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.HasOne(workspaceMember => workspaceMember.Workspace)
            .WithMany(workspace => workspace.Members)
            .HasForeignKey(workspaceMember => workspaceMember.WorkspaceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(workspaceMember => workspaceMember.User)
            .WithMany(user => user.WorkspaceMemberships)
            .HasForeignKey(workspaceMember => workspaceMember.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(workspaceMember => new
            {
                workspaceMember.WorkspaceId,
                workspaceMember.UserId
            })
            .IsUnique();

        builder.HasIndex(workspaceMember => workspaceMember.UserId);
    }
}
