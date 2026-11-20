using clean.Application.Common.Interfaces;
using clean.Domain.Entities.Organization;
using Microsoft.EntityFrameworkCore;

namespace clean.Infrastructure.Persistence;

public sealed class OrganizationDbContext(DbContextOptions<OrganizationDbContext> options)
    : DbContext(options), IOrganizationDbContext
{
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Division> Divisions => Set<Division>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<CostCenter> CostCenters => Set<CostCenter>();
    public DbSet<BusinessUnit> BusinessUnits => Set<BusinessUnit>();

    public Task<Company?> LockCompanyAsync(long id, CancellationToken cancellationToken) =>
        Companies.FromSqlInterpolated($"""SELECT * FROM "DbCompany" WHERE "Id" = {id} FOR UPDATE""")
            .SingleOrDefaultAsync(cancellationToken);

    public Task<Branch?> LockBranchAsync(long id, CancellationToken cancellationToken) =>
        Branches.FromSqlInterpolated($"""SELECT * FROM "DbBranch" WHERE "Id" = {id} FOR UPDATE""")
            .SingleOrDefaultAsync(cancellationToken);

    public Task<Division?> LockDivisionAsync(long id, CancellationToken cancellationToken) =>
        Divisions.FromSqlInterpolated($"""SELECT * FROM "DbDivision" WHERE "Id" = {id} FOR UPDATE""")
            .SingleOrDefaultAsync(cancellationToken);

    public Task<Department?> LockDepartmentAsync(long id, CancellationToken cancellationToken) =>
        Departments.FromSqlInterpolated($"""SELECT * FROM "DbDepartment" WHERE "Id" = {id} FOR UPDATE""")
            .SingleOrDefaultAsync(cancellationToken);

    public Task<Position?> LockPositionAsync(long id, CancellationToken cancellationToken) =>
        Positions.FromSqlInterpolated($"""SELECT * FROM "DbPosition" WHERE "Id" = {id} FOR UPDATE""")
            .SingleOrDefaultAsync(cancellationToken);

    public Task<CostCenter?> LockCostCenterAsync(long id, CancellationToken cancellationToken) =>
        CostCenters.FromSqlInterpolated($"""SELECT * FROM "DbCostCenter" WHERE "Id" = {id} FOR UPDATE""")
            .SingleOrDefaultAsync(cancellationToken);

    public Task<BusinessUnit?> LockBusinessUnitAsync(long id, CancellationToken cancellationToken) =>
        BusinessUnits.FromSqlInterpolated($"""SELECT * FROM "DbBusinessUnit" WHERE "Id" = {id} FOR UPDATE""")
            .SingleOrDefaultAsync(cancellationToken);

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Company>(entity =>
        {
            entity.ToTable("DbCompany", table =>
            {
                table.HasCheckConstraint("CK_DbCompany_Code", """ "Code" ~ '^[A-Z0-9_-]{1,50}$' """);
                table.HasCheckConstraint("CK_DbCompany_Name", """ length(btrim("Name")) > 0 """);
            });
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).UseIdentityAlwaysColumn();
            entity.Property(x => x.Code).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasIndex(x => x.Code).IsUnique();
        });

        model.Entity<Branch>(entity =>
        {
            entity.ToTable("DbBranch", table =>
            {
                table.HasCheckConstraint("CK_DbBranch_Code", """ "Code" ~ '^[A-Z0-9_-]{1,50}$' """);
                table.HasCheckConstraint("CK_DbBranch_Name", """ length(btrim("Name")) > 0 """);
            });
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.Id, x.CompanyId });
            entity.Property(x => x.Id).UseIdentityAlwaysColumn();
            entity.Property(x => x.Code).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
            entity.HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        });

        model.Entity<Division>(entity =>
        {
            entity.ToTable("DbDivision", table =>
            {
                table.HasCheckConstraint("CK_DbDivision_Code", """ "Code" ~ '^[A-Z0-9_-]{1,50}$' """);
                table.HasCheckConstraint("CK_DbDivision_Name", """ length(btrim("Name")) > 0 """);
            });
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.Id, x.CompanyId });
            entity.Property(x => x.Id).UseIdentityAlwaysColumn();
            entity.Property(x => x.Code).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
            entity.HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Branch>().WithMany()
                .HasForeignKey(x => new { x.BranchId, x.CompanyId })
                .HasPrincipalKey(x => new { x.Id, x.CompanyId })
                .OnDelete(DeleteBehavior.Restrict);
        });

        model.Entity<Department>(entity =>
        {
            entity.ToTable("DbDepartment", table =>
            {
                table.HasCheckConstraint("CK_DbDepartment_Code", """ "Code" ~ '^[A-Z0-9_-]{1,50}$' """);
                table.HasCheckConstraint("CK_DbDepartment_Name", """ length(btrim("Name")) > 0 """);
            });
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).UseIdentityAlwaysColumn();
            entity.Property(x => x.Code).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
            entity.HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Division>().WithMany()
                .HasForeignKey(x => new { x.DivisionId, x.CompanyId })
                .HasPrincipalKey(x => new { x.Id, x.CompanyId })
                .OnDelete(DeleteBehavior.Restrict);
        });

        model.Entity<CostCenter>(entity =>
        {
            entity.ToTable("DbCostCenter", table =>
            {
                table.HasCheckConstraint("CK_DbCostCenter_Code", """ "Code" ~ '^[A-Z0-9_-]{1,50}$' """);
                table.HasCheckConstraint("CK_DbCostCenter_Name", """ length(btrim("Name")) > 0 """);
            });
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).UseIdentityAlwaysColumn();
            entity.Property(x => x.Code).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
            entity.HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        });

        model.Entity<Position>(entity =>
        {
            entity.ToTable("DbPosition", table =>
            {
                table.HasCheckConstraint("CK_DbPosition_Code", """ "Code" ~ '^[A-Z0-9_-]{1,50}$' """);
                table.HasCheckConstraint("CK_DbPosition_Name", """ length(btrim("Name")) > 0 """);
            });
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).UseIdentityAlwaysColumn();
            entity.Property(x => x.Code).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasIndex(x => x.Code).IsUnique();
        });

        model.Entity<BusinessUnit>(entity =>
        {
            entity.ToTable("DbBusinessUnit", table =>
            {
                table.HasCheckConstraint("CK_DbBusinessUnit_Code", """ "Code" ~ '^[A-Z0-9_-]{1,50}$' """);
                table.HasCheckConstraint("CK_DbBusinessUnit_Name", """ length(btrim("Name")) > 0 """);
            });
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).UseIdentityAlwaysColumn();
            entity.Property(x => x.Code).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasIndex(x => x.Code).IsUnique();
        });
    }
}
