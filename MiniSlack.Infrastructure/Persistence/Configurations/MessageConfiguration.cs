using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniSlack.Domain.Messages;

namespace MiniSlack.Infrastructure.Persistence.Configurations;

public sealed class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("messages");

        builder.Property(message => message.Content)
            .IsRequired();

        builder.Property(message => message.Type)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.HasOne(message => message.Conversation)
            .WithMany(conversation => conversation.Messages)
            .HasForeignKey(message => message.ConversationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(message => message.Sender)
            .WithMany(user => user.Messages)
            .HasForeignKey(message => message.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(message => message.ParentMessage)
            .WithMany(message => message.Replies)
            .HasForeignKey(message => message.ParentMessageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(message => new
        {
            message.ConversationId,
            message.CreatedAtUtc
        });

        builder.HasIndex(message => message.SenderId);
    }
}
