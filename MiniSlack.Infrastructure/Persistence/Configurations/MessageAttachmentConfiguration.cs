using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniSlack.Domain.Messages;

namespace MiniSlack.Infrastructure.Persistence.Configurations;

public sealed class MessageAttachmentConfiguration : IEntityTypeConfiguration<MessageAttachment>
{
    public void Configure(EntityTypeBuilder<MessageAttachment> builder)
    {
        builder.ToTable("message_attachments");

        builder.Property(messageAttachment => messageAttachment.FileName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(messageAttachment => messageAttachment.ContentType)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(messageAttachment => messageAttachment.BlobUrl)
            .HasMaxLength(2048)
            .IsRequired();

        builder.HasOne(messageAttachment => messageAttachment.Message)
            .WithMany(message => message.Attachments)
            .HasForeignKey(messageAttachment => messageAttachment.MessageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(messageAttachment => messageAttachment.MessageId);
    }
}
