using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniSlack.Domain.Conversations;

namespace MiniSlack.Infrastructure.Persistence.Configurations;

public sealed class ConversationMemberConfiguration : IEntityTypeConfiguration<ConversationMember>
{
    public void Configure(EntityTypeBuilder<ConversationMember> builder)
    {
        builder.ToTable("conversation_members");

        builder.Property(conversationMember => conversationMember.Role)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.HasOne(conversationMember => conversationMember.Conversation)
            .WithMany(conversation => conversation.Members)
            .HasForeignKey(conversationMember => conversationMember.ConversationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(conversationMember => conversationMember.User)
            .WithMany(user => user.ConversationMemberships)
            .HasForeignKey(conversationMember => conversationMember.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(conversationMember => conversationMember.LastReadMessage)
            .WithMany()
            .HasForeignKey(conversationMember => conversationMember.LastReadMessageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(conversationMember => new
            {
                conversationMember.ConversationId,
                conversationMember.UserId
            })
            .IsUnique();

        builder.HasIndex(conversationMember => conversationMember.UserId);
    }
}
