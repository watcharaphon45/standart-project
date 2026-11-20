using clean.Domain.Entities.Organization;
using Microsoft.EntityFrameworkCore;

namespace clean.Application.Common.Interfaces;

public interface IOrganizationDbContext
{
    DbSet<Company> Companies { get; }
    DbSet<Branch> Branches { get; }
    DbSet<Division> Divisions { get; }
    DbSet<Department> Departments { get; }
    DbSet<Position> Positions { get; }
    DbSet<CostCenter> CostCenters { get; }
    DbSet<BusinessUnit> BusinessUnits { get; }

    Task<Company?> LockCompanyAsync(long id, CancellationToken cancellationToken);
    Task<Branch?> LockBranchAsync(long id, CancellationToken cancellationToken);
    Task<Division?> LockDivisionAsync(long id, CancellationToken cancellationToken);
    Task<Department?> LockDepartmentAsync(long id, CancellationToken cancellationToken);
    Task<Position?> LockPositionAsync(long id, CancellationToken cancellationToken);
    Task<CostCenter?> LockCostCenterAsync(long id, CancellationToken cancellationToken);
    Task<BusinessUnit?> LockBusinessUnitAsync(long id, CancellationToken cancellationToken);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
