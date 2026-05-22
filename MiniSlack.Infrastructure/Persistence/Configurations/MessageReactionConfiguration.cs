using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniSlack.Domain.Messages;

namespace MiniSlack.Infrastructure.Persistence.Configurations;

public sealed class MessageReactionConfiguration : IEntityTypeConfiguration<MessageReaction>
{
    public void Configure(EntityTypeBuilder<MessageReaction> builder)
    {
        builder.ToTable("message_reactions");

        builder.Property(messageReaction => messageReaction.Emoji)
            .HasMaxLength(64)
            .IsRequired();

        builder.HasOne(messageReaction => messageReaction.Message)
            .WithMany(message => message.Reactions)
            .HasForeignKey(messageReaction => messageReaction.MessageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(messageReaction => messageReaction.User)
            .WithMany(user => user.MessageReactions)
            .HasForeignKey(messageReaction => messageReaction.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(messageReaction => new
            {
                messageReaction.MessageId,
                messageReaction.UserId,
                messageReaction.Emoji
            })
            .IsUnique();
    }
}
