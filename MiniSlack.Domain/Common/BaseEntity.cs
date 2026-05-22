namespace MiniSlack.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }

    public Guid? UpdatedByUserId { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAtUtc { get; set; }

    public Guid? DeletedByUserId { get; set; }
}
