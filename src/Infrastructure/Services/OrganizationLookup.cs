using System.Globalization;
using clean.Application.Common.Interfaces;
using clean.Application.Common.Models;
using clean.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace clean.Infrastructure.Services;

public sealed class OrganizationLookup(OrganizationDbContext db) : IOrganizationLookup
{
    public async Task<IReadOnlyList<CompanyOption>> CompaniesAsync(IReadOnlyCollection<long> companyIds, bool all, CancellationToken cancellationToken)
    {
        var query = db.Companies.AsNoTracking();
        if (!all) query = query.Where(x => companyIds.Contains(x.Id));
        var companies = await query.OrderBy(x => x.Code).ToListAsync(cancellationToken);
        return companies.Select(x => new CompanyOption(x.Id.ToString(CultureInfo.InvariantCulture), x.Code, x.Name, x.IsActive)).ToArray();
    }

    public async Task<CompanyOption?> CompanyAsync(long companyId, CancellationToken cancellationToken)
    {
        var company = await db.Companies.AsNoTracking().SingleOrDefaultAsync(x => x.Id == companyId, cancellationToken);
        return company is null
            ? null
            : new CompanyOption(company.Id.ToString(CultureInfo.InvariantCulture), company.Code, company.Name, company.IsActive);
    }

    public async Task<IReadOnlyList<LookupOptionDto>> BranchesByCompanyAsync(long companyId, CancellationToken cancellationToken)
    {
        var branches = await db.Branches.AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .OrderBy(x => x.Code)
            .ToListAsync(cancellationToken);
        return branches.Select(x => new LookupOptionDto(
            x.Id.ToString(CultureInfo.InvariantCulture),
            x.Code,
            x.Name,
            x.IsActive)).ToArray();
    }

    public async Task<IReadOnlyList<DepartmentScopeOptionDto>> DepartmentsByCompanyAsync(long companyId, CancellationToken cancellationToken)
    {
        var rows = await (from department in db.Departments.AsNoTracking()
                          join division in db.Divisions.AsNoTracking() on department.DivisionId equals division.Id
                          join branch in db.Branches.AsNoTracking() on division.BranchId equals branch.Id
                          where department.CompanyId == companyId
                          orderby branch.Code, division.Code, department.Code
                          select new { branch, division, department }).ToListAsync(cancellationToken);
        return rows.Select(x => new DepartmentScopeOptionDto(
            x.department.Id.ToString(CultureInfo.InvariantCulture),
            x.branch.Id.ToString(CultureInfo.InvariantCulture),
            x.branch.Code,
            x.branch.Name,
            x.division.Id.ToString(CultureInfo.InvariantCulture),
            x.division.Code,
            x.division.Name,
            x.department.Code,
            x.department.Name,
            x.department.IsActive)).ToArray();
    }

    public async Task<IReadOnlySet<long>> DepartmentIdsByBranchesAsync(
        long companyId,
        IReadOnlyCollection<long> branchIds,
        CancellationToken cancellationToken)
    {
        if (branchIds.Count == 0) return new HashSet<long>();
        var ids = await (from department in db.Departments.AsNoTracking()
                         join division in db.Divisions.AsNoTracking() on department.DivisionId equals division.Id
                         where department.CompanyId == companyId && branchIds.Contains(division.BranchId)
                         select department.Id).ToArrayAsync(cancellationToken);
        return ids.ToHashSet();
    }

    public async Task<IReadOnlySet<long>> DivisionIdsByBranchesAsync(
        long companyId,
        IReadOnlyCollection<long> branchIds,
        CancellationToken cancellationToken)
    {
        if (branchIds.Count == 0) return new HashSet<long>();
        var ids = await db.Divisions.AsNoTracking()
            .Where(x => x.CompanyId == companyId && branchIds.Contains(x.BranchId))
            .Select(x => x.Id)
            .ToArrayAsync(cancellationToken);
        return ids.ToHashSet();
    }
}
