using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Lehrerleicht.Approval.Core.Entities;

namespace Lehrerleicht.Approval.Infrastructure.Data;

public class ApprovalDbContext : IdentityDbContext<Teacher>
{
    public ApprovalDbContext(DbContextOptions<ApprovalDbContext> options)
        : base(options)
    {
    }

    public DbSet<School> Schools => Set<School>();
    public DbSet<ApprovalEntity> Approvals => Set<ApprovalEntity>();
    public DbSet<PendingAction> PendingActions => Set<PendingAction>();
    public DbSet<ActionOption> ActionOptions => Set<ActionOption>();
    public DbSet<ActionHistory> ActionHistories => Set<ActionHistory>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(ApprovalDbContext).Assembly);

        builder.Entity<Teacher>().ToTable("teachers");
        builder.Entity<School>().ToTable("schools");
        builder.Entity<ApprovalEntity>().ToTable("approvals");
        builder.Entity<PendingAction>().ToTable("pending_actions");
        builder.Entity<ActionOption>().ToTable("action_options");
        builder.Entity<ActionHistory>().ToTable("action_histories");
        builder.Entity<NotificationPreference>().ToTable("notification_preferences");
    }
}
