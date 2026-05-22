using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniSlack.Domain.Workspaces;

namespace MiniSlack.Infrastructure.Persistence.Configurations;

public sealed class WorkspaceConfiguration : IEntityTypeConfiguration<Workspace>
{
    public void Configure(EntityTypeBuilder<Workspace> builder)
    {
        builder.ToTable("workspaces");

        builder.Property(workspace => workspace.Name)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(workspace => workspace.Slug)
            .HasMaxLength(80)
            .IsRequired();

        builder.HasOne(workspace => workspace.OwnerUser)
            .WithMany()
            .HasForeignKey(workspace => workspace.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(workspace => workspace.Slug)
            .IsUnique();
    }
}
