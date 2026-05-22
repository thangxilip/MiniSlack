using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniSlack.Domain.Conversations;

namespace MiniSlack.Infrastructure.Persistence.Configurations;

public sealed class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("conversations");

        builder.Property(conversation => conversation.Type)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(conversation => conversation.Name)
            .HasMaxLength(120);

        builder.Property(conversation => conversation.Description)
            .HasMaxLength(512);

        builder.HasOne(conversation => conversation.Workspace)
            .WithMany(workspace => workspace.Conversations)
            .HasForeignKey(conversation => conversation.WorkspaceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(conversation => conversation.CreatorUser)
            .WithMany()
            .HasForeignKey(conversation => conversation.CreatorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(conversation => new
        {
            conversation.WorkspaceId,
            conversation.Type
        });

        builder.HasIndex(conversation => new
        {
            conversation.WorkspaceId,
            conversation.Name
        });
    }
}
