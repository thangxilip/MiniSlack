using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using MiniSlack.Domain.Auth;
using MiniSlack.Domain.Common;
using MiniSlack.Domain.Conversations;
using MiniSlack.Domain.Messages;
using MiniSlack.Domain.Users;
using MiniSlack.Domain.Workspaces;

namespace MiniSlack.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    private readonly HashSet<BaseEntity> _hardDeletedEntities = [];

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Workspace> Workspaces => Set<Workspace>();

    public DbSet<WorkspaceMember> WorkspaceMembers => Set<WorkspaceMember>();

    public DbSet<Conversation> Conversations => Set<Conversation>();

    public DbSet<ConversationMember> ConversationMembers => Set<ConversationMember>();

    public DbSet<Message> Messages => Set<Message>();

    public DbSet<MessageReaction> MessageReactions => Set<MessageReaction>();

    public DbSet<MessageAttachment> MessageAttachments => Set<MessageAttachment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        ApplySoftDeleteQueryFilter(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    public override int SaveChanges()
    {
        ApplyAuditInfo();

        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditInfo();

        return base.SaveChangesAsync(cancellationToken);
    }

    public void MarkAsHardDeleted(BaseEntity entity)
    {
        _hardDeletedEntities.Add(entity);
    }

    private void ApplyAuditInfo()
    {
        var utcNow = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    if (entry.Entity.CreatedAtUtc == default)
                    {
                        entry.Entity.CreatedAtUtc = utcNow;
                    }
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAtUtc = utcNow;
                    break;

                case EntityState.Deleted:
                    if (_hardDeletedEntities.Contains(entry.Entity))
                    {
                        break;
                    }

                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedAtUtc = utcNow;
                    break;
            }
        }

        _hardDeletedEntities.Clear();
    }

    private static void ApplySoftDeleteQueryFilter(ModelBuilder modelBuilder)
    {
        var softDeleteFilter = (Expression<Func<BaseEntity, bool>>)(entity => !entity.IsDeleted);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            var parameter = Expression.Parameter(entityType.ClrType, "entity");

            var softDeleteBody = ReplacingExpressionVisitor.Replace(
                softDeleteFilter.Parameters.Single(),
                parameter,
                softDeleteFilter.Body);

            var existingFilter = entityType.GetQueryFilter();
            if (existingFilter is null)
            {
                modelBuilder.Entity(entityType.ClrType)
                    .HasQueryFilter(Expression.Lambda(softDeleteBody, parameter));

                continue;
            }

            var existingBody = ReplacingExpressionVisitor.Replace(
                existingFilter.Parameters.Single(),
                parameter,
                existingFilter.Body);

            modelBuilder.Entity(entityType.ClrType)
                .HasQueryFilter(Expression.Lambda(Expression.AndAlso(existingBody, softDeleteBody), parameter));
        }
    }
}
