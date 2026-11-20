using clean.Domain.Entities.System;
using clean.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace clean.Infrastructure.Persistence;

public sealed class SystemDbContext(DbContextOptions<SystemDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, long>(options)
{
    public DbSet<CompanyAssignment> CompanyAssignments => Set<CompanyAssignment>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserSession> Sessions => Set<UserSession>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<DocumentSequence> DocumentSequences => Set<DocumentSequence>();
    public DbSet<ApprovalWorkflow> ApprovalWorkflows => Set<ApprovalWorkflow>();
    public DbSet<ApprovalWorkflowStep> ApprovalWorkflowSteps => Set<ApprovalWorkflowStep>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        base.OnModelCreating(model);
        model.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("SuUser");
            entity.Property(x => x.Id).UseIdentityAlwaysColumn();
            entity.Property(x => x.Version).HasDefaultValue(1L);
            entity.HasIndex(x => x.NormalizedEmail).IsUnique();
        });
        model.Entity<ApplicationRole>(entity =>
        {
            entity.ToTable("SuRole");
            entity.Property(x => x.Id).UseIdentityAlwaysColumn();
            entity.Property(x => x.Description).HasMaxLength(200).HasDefaultValue("");
            entity.Property(x => x.CodeLocked).HasDefaultValue(false);
            entity.Property(x => x.Version).HasDefaultValue(1L);
            entity.HasIndex(x => x.NormalizedName)
                .HasDatabaseName("RoleNameIndex")
                .IsUnique()
                .HasFilter(""" "CompanyId" IS NULL """);
            entity.HasIndex(x => new { x.CompanyId, x.NormalizedName })
                .HasDatabaseName("IX_SuRole_CompanyId_NormalizedName")
                .IsUnique()
                .HasFilter(""" "CompanyId" IS NOT NULL """);
            entity.HasIndex(x => new { x.Id, x.CompanyId }).IsUnique();
        });
        model.Entity<IdentityUserRole<long>>().ToTable("SuUserRole");
        model.Entity<IdentityUserClaim<long>>().ToTable("SuUserClaim");
        model.Entity<IdentityUserLogin<long>>().ToTable("SuUserLogin");
        model.Entity<IdentityRoleClaim<long>>().ToTable("SuRoleClaim");
        model.Entity<IdentityUserToken<long>>().ToTable("SuUserToken");
        model.Entity<CompanyAssignment>(entity =>
        {
            entity.ToTable("SuCompanyAssignment");
            entity.HasKey(x => new { x.UserId, x.CompanyId });
            entity.Property(x => x.ScopeType).HasMaxLength(20);
            entity.Property(x => x.Version).HasDefaultValue(1L);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationRole>().WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<RolePermission>(entity =>
        {
            entity.ToTable("SuRolePermission");
            entity.HasKey(x => new { x.RoleId, x.Permission });
            entity.Property(x => x.Permission).HasMaxLength(100);
            entity.HasOne<ApplicationRole>().WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<UserSession>(entity =>
        {
            entity.ToTable("SuSession");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(32);
            entity.Property(x => x.AuthorizationFingerprint).HasMaxLength(64);
            entity.Property(x => x.SecurityStamp).HasMaxLength(100);
            entity.HasIndex(x => new { x.UserId, x.Revoked });
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<AuditEvent>(entity =>
        {
            entity.ToTable("SuAuditEvent");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).UseIdentityAlwaysColumn();
            entity.Property(x => x.ProgramCode).HasMaxLength(20);
            entity.Property(x => x.Action).HasMaxLength(50);
            entity.Property(x => x.Result).HasMaxLength(30);
            entity.Property(x => x.TraceId).HasMaxLength(100);
            entity.Property(x => x.Before).HasColumnType("jsonb");
            entity.Property(x => x.After).HasColumnType("jsonb");
            entity.HasIndex(x => new { x.CompanyId, x.OccurredAtUtc });
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<MenuItem>(entity =>
        {
            entity.ToTable("SuMenuItem");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).UseIdentityAlwaysColumn();
            entity.Property(x => x.ModuleCode).HasMaxLength(10).IsRequired();
            entity.Property(x => x.ProgramCode).HasMaxLength(20).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Route).HasMaxLength(100).IsRequired();
            entity.Property(x => x.RequiredPermission).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Version).HasDefaultValue(1L);
            entity.HasIndex(x => x.ProgramCode).IsUnique();
        });
        model.Entity<DocumentSequence>(entity =>
        {
            entity.ToTable("SuDocumentSequence");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).UseIdentityAlwaysColumn();
            entity.Property(x => x.Code).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Prefix).HasMaxLength(20).IsRequired();
            entity.Property(x => x.ResetPolicy).HasMaxLength(20).IsRequired();
            entity.Property(x => x.CurrentPeriod).HasMaxLength(10).IsRequired();
            entity.Property(x => x.Version).HasDefaultValue(1L);
            entity.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
        });
        model.Entity<ApprovalWorkflow>(entity =>
        {
            entity.ToTable("SuApprovalWorkflow");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).UseIdentityAlwaysColumn();
            entity.Property(x => x.Code).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.DocumentType).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Version).HasDefaultValue(1L);
            entity.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
        });
        model.Entity<ApprovalWorkflowStep>(entity =>
        {
            entity.ToTable("SuApprovalWorkflowStep");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).UseIdentityAlwaysColumn();
            entity.Property(x => x.StepName).HasMaxLength(200).IsRequired();
            entity.HasIndex(x => new { x.WorkflowId, x.StepOrder }).IsUnique();
            entity.HasOne<ApprovalWorkflow>().WithMany().HasForeignKey(x => x.WorkflowId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<ApplicationRole>().WithMany().HasForeignKey(x => x.ApproverRoleId).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<SystemSetting>(entity =>
        {
            entity.ToTable("SuSystemSetting");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).UseIdentityAlwaysColumn();
            entity.Property(x => x.Key).HasMaxLength(50).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.DataType).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Value).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Version).HasDefaultValue(1L);
            entity.HasIndex(x => x.Key)
                .HasDatabaseName("IX_SuSystemSetting_Global_Key")
                .IsUnique()
                .HasFilter(""" "CompanyId" IS NULL """);
            entity.HasIndex(x => new { x.CompanyId, x.Key })
                .HasDatabaseName("IX_SuSystemSetting_CompanyId_Key")
                .IsUnique()
                .HasFilter(""" "CompanyId" IS NOT NULL """);
        });
    }
}
