using System.Globalization;
using clean.Application.Common.Behaviors;
using clean.Application.Common.Interfaces;
using clean.Application.Common.Models;
using clean.Application.Features.DB.Shared;
using clean.Domain.Entities.Organization;
using clean.Domain.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace clean.Application.Features.DB.DBRT03;

public sealed record SearchDepartmentsRequest(
    string CompanyId,
    string? BranchId = null,
    string? DivisionId = null,
    string? Keyword = null,
    bool? IsActive = null,
    int PageNumber = 1,
    int PageSize = 25,
    string SortField = "code",
    string SortDirection = "asc") : IRequest<Page<DepartmentDto>>;

public sealed class SearchDepartmentsValidator : AbstractValidator<SearchDepartmentsRequest>
{
    public SearchDepartmentsValidator()
    {
        RuleFor(x => x.CompanyId).ValidId("กรุณาเลือกบริษัท");
        RuleFor(x => x.BranchId).ValidOptionalId("รหัสสาขาไม่ถูกต้อง");
        RuleFor(x => x.DivisionId).ValidOptionalId("รหัสฝ่ายไม่ถูกต้อง");
        OrganizationMasterValidation.ApplyPagingRules(
            this,
            x => x.Keyword,
            x => x.PageNumber,
            x => x.PageSize,
            x => x.SortField,
            x => x.SortDirection,
            "code", "name", "branchCode", "divisionCode", "isActive");
    }
}

public sealed class SearchDepartmentsHandler(IOrganizationDbContext db, IAccessService access)
    : IRequestHandler<SearchDepartmentsRequest, Page<DepartmentDto>>
{
    public async Task<Page<DepartmentDto>> Handle(SearchDepartmentsRequest request, CancellationToken cancellationToken)
    {
        var companyId = IdValue.Parse(request.CompanyId);
        var scope = await access.GetScopeFilterAsync(companyId, "DBRT03.VIEW", cancellationToken);
        var query = from department in db.Departments.AsNoTracking()
                    join division in db.Divisions.AsNoTracking() on department.DivisionId equals division.Id
                    join branch in db.Branches.AsNoTracking() on division.BranchId equals branch.Id
                    where department.CompanyId == companyId
                    select new { department, division, branch };

        if (!scope.FullCompany)
            query = query.Where(x =>
                scope.BranchIds.Contains(x.division.BranchId) || scope.DepartmentIds.Contains(x.department.Id));
        if (!string.IsNullOrEmpty(request.BranchId))
        {
            var branchId = IdValue.Parse(request.BranchId);
            query = query.Where(x => x.division.BranchId == branchId);
        }
        if (!string.IsNullOrEmpty(request.DivisionId))
        {
            var divisionId = IdValue.Parse(request.DivisionId);
            query = query.Where(x => x.department.DivisionId == divisionId);
        }
        if (request.IsActive is not null)
            query = query.Where(x => x.department.IsActive == request.IsActive);
        var keyword = request.Keyword?.Trim();
        if (!string.IsNullOrEmpty(keyword))
        {
            var upper = keyword.ToUpperInvariant();
            query = query.Where(x =>
                x.department.Code.Contains(upper)
                || x.department.Name.ToUpper().Contains(upper)
                || x.division.Code.Contains(upper)
                || x.division.Name.ToUpper().Contains(upper)
                || x.branch.Code.Contains(upper)
                || x.branch.Name.ToUpper().Contains(upper));
        }

        var count = await query.CountAsync(cancellationToken);
        var ascending = request.SortDirection == "asc";
        var sorted = (request.SortField, ascending) switch
        {
            ("name", true) => query.OrderBy(x => x.department.Name),
            ("name", false) => query.OrderByDescending(x => x.department.Name),
            ("branchCode", true) => query.OrderBy(x => x.branch.Code),
            ("branchCode", false) => query.OrderByDescending(x => x.branch.Code),
            ("divisionCode", true) => query.OrderBy(x => x.division.Code),
            ("divisionCode", false) => query.OrderByDescending(x => x.division.Code),
            ("isActive", true) => query.OrderBy(x => x.department.IsActive),
            ("isActive", false) => query.OrderByDescending(x => x.department.IsActive),
            (_, true) => query.OrderBy(x => x.department.Code),
            _ => query.OrderByDescending(x => x.department.Code)
        };
        var rows = await sorted.ThenBy(x => x.department.Id)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new(
            rows.Select(x => DepartmentDto.Of(
                x.department, x.branch.Id, x.branch.Code, x.division.Code, x.division.Name)).ToArray(),
            count,
            request.PageNumber,
            request.PageSize);
    }
}

public sealed record GetDepartmentOptionsRequest(string CompanyId) : IRequest<DepartmentOptionsDto>;

public sealed class GetDepartmentOptionsValidator : AbstractValidator<GetDepartmentOptionsRequest>
{
    public GetDepartmentOptionsValidator() => RuleFor(x => x.CompanyId).ValidId("กรุณาเลือกบริษัท");
}

public sealed class GetDepartmentOptionsHandler(IOrganizationDbContext db, IAccessService access)
    : IRequestHandler<GetDepartmentOptionsRequest, DepartmentOptionsDto>
{
    public async Task<DepartmentOptionsDto> Handle(GetDepartmentOptionsRequest request, CancellationToken cancellationToken)
    {
        var companyId = IdValue.Parse(request.CompanyId);
        var scope = await access.GetScopeFilterAnyAsync(
            companyId,
            ["DBRT03.VIEW", "DBRT03.CREATE", "DBRT03.UPDATE"],
            cancellationToken);

        var divisionQuery = from division in db.Divisions.AsNoTracking()
                            where division.CompanyId == companyId
                            select division;
        if (!scope.FullCompany)
        {
            var deptDivisionIds = await db.Departments.AsNoTracking()
                .Where(d => d.CompanyId == companyId && scope.DepartmentIds.Contains(d.Id))
                .Select(d => d.DivisionId)
                .Distinct()
                .ToArrayAsync(cancellationToken);
            divisionQuery = divisionQuery.Where(x =>
                scope.BranchIds.Contains(x.BranchId) || deptDivisionIds.Contains(x.Id));
        }

        var divisions = await divisionQuery.OrderBy(x => x.Code).ToListAsync(cancellationToken);
        var branchQuery = db.Branches.AsNoTracking().Where(x => x.CompanyId == companyId);
        if (!scope.FullCompany)
        {
            var visibleBranchIds = divisions.Select(x => x.BranchId).Concat(scope.BranchIds).Distinct().ToArray();
            branchQuery = branchQuery.Where(x => visibleBranchIds.Contains(x.Id));
        }
        var branches = await branchQuery.OrderBy(x => x.Code).ToListAsync(cancellationToken);

        return new DepartmentOptionsDto(
            branches.Select(x => new LookupOptionDto(
                x.Id.ToString(CultureInfo.InvariantCulture), x.Code, x.Name, x.IsActive)).ToArray(),
            divisions.Select(x => new DivisionLookupOptionDto(
                x.Id.ToString(CultureInfo.InvariantCulture),
                x.BranchId.ToString(CultureInfo.InvariantCulture),
                x.Code,
                x.Name,
                x.IsActive)).ToArray());
    }
}

public sealed record GetDepartmentDetailRequest(string Id) : IRequest<DepartmentDto>;

public sealed class GetDepartmentDetailValidator : AbstractValidator<GetDepartmentDetailRequest>
{
    public GetDepartmentDetailValidator() => RuleFor(x => x.Id).ValidId();
}

public sealed class GetDepartmentDetailHandler(IOrganizationDbContext db, IAccessService access)
    : IRequestHandler<GetDepartmentDetailRequest, DepartmentDto>
{
    public async Task<DepartmentDto> Handle(GetDepartmentDetailRequest request, CancellationToken cancellationToken)
    {
        var id = IdValue.Parse(request.Id);
        var row = await (from department in db.Departments.AsNoTracking()
                         join division in db.Divisions.AsNoTracking() on department.DivisionId equals division.Id
                         join branch in db.Branches.AsNoTracking() on division.BranchId equals branch.Id
                         where department.Id == id
                         select new { department, division, branch }).SingleOrDefaultAsync(cancellationToken)
            ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลแผนก", 404);

        await access.DemandDepartmentAsync(
            row.department.CompanyId, row.branch.Id, row.department.Id, "DBRT03.VIEW", cancellationToken);
        return DepartmentDto.Of(row.department, row.branch.Id, row.branch.Code, row.division.Code, row.division.Name);
    }
}

public sealed class CreateDepartmentRequest : IRequest<DepartmentDto>, INormalizableRequest
{
    public string CompanyId { get; set; } = "";
    public string DivisionId { get; set; } = "";
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public void Normalize() => Code = Company.NormalizeCode(Code);
}

public sealed class CreateDepartmentValidator : AbstractValidator<CreateDepartmentRequest>
{
    public CreateDepartmentValidator()
    {
        RuleFor(x => x.CompanyId).ValidId("กรุณาเลือกบริษัท");
        RuleFor(x => x.DivisionId).ValidId("กรุณาเลือกฝ่าย");
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).ValidName();
    }
}

public sealed class CreateDepartmentHandler(
    IOrganizationDbContext db,
    IAccessService access,
    ICurrentActor actor,
    IAtomicWriter transaction,
    IAuditWriter audit,
    TimeProvider clock) : IRequestHandler<CreateDepartmentRequest, DepartmentDto>
{
    public async Task<DepartmentDto> Handle(CreateDepartmentRequest request, CancellationToken cancellationToken)
    {
        var companyId = IdValue.Parse(request.CompanyId);
        var divisionId = IdValue.Parse(request.DivisionId);
        var target = await db.Divisions.AsNoTracking()
            .Where(x => x.Id == divisionId && x.CompanyId == companyId)
            .Select(x => new { x.BranchId })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new RuleException("DIVISION_NOT_FOUND", "ไม่พบฝ่ายในบริษัทที่เลือก", 404);
        await access.DemandBranchAsync(companyId, target.BranchId, "DBRT03.CREATE", cancellationToken);

        return await transaction.ExecuteAsync(async () =>
        {
            var division = await db.LockDivisionAsync(divisionId, cancellationToken);
            if (division is null || division.CompanyId != companyId)
                throw new RuleException("DIVISION_NOT_FOUND", "ไม่พบฝ่ายในบริษัทที่เลือก", 404);
            if (!division.IsActive)
                throw new RuleException("PARENT_INACTIVE", "กรุณาเปิดใช้งานฝ่ายก่อน");

            var branch = await db.Branches.AsNoTracking().SingleAsync(x => x.Id == division.BranchId, cancellationToken);
            var department = Department.Create(
                companyId, divisionId, request.Code, request.Name, actor.UserId, clock.GetUtcNow());
            db.Departments.Add(department);
            await db.SaveChangesAsync(cancellationToken);
            await audit.RecordOrganizationAsync(
                "DBRT03", "CREATE", department.CompanyId, department.Id, null, DepartmentSnapshot.Of(department), cancellationToken);
            return DepartmentDto.Of(department, branch.Id, branch.Code, division.Code, division.Name);
        }, cancellationToken);
    }
}

public sealed class UpdateDepartmentRequest : IRequest<DepartmentDto>, INormalizableRequest
{
    public string Id { get; set; } = "";
    public string DivisionId { get; set; } = "";
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public void Normalize() => Code = Company.NormalizeCode(Code);
}

public sealed class UpdateDepartmentValidator : AbstractValidator<UpdateDepartmentRequest>
{
    public UpdateDepartmentValidator()
    {
        RuleFor(x => x.Id).ValidId();
        RuleFor(x => x.DivisionId).ValidId("กรุณาเลือกฝ่าย");
        RuleFor(x => x.Version).ValidId("กรุณาโหลดข้อมูลล่าสุดก่อนบันทึก");
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).ValidName();
    }
}

public sealed class UpdateDepartmentHandler(
    IOrganizationDbContext db,
    IAccessService access,
    ICurrentActor actor,
    IAtomicWriter transaction,
    IAuditWriter audit,
    TimeProvider clock) : IRequestHandler<UpdateDepartmentRequest, DepartmentDto>
{
    public async Task<DepartmentDto> Handle(UpdateDepartmentRequest request, CancellationToken cancellationToken)
    {
        var id = IdValue.Parse(request.Id);
        var nextDivisionId = IdValue.Parse(request.DivisionId);
        var current = await (from department in db.Departments.AsNoTracking()
                             join division in db.Divisions.AsNoTracking() on department.DivisionId equals division.Id
                             where department.Id == id
                             select new { department.CompanyId, department.DivisionId, division.BranchId })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลแผนก", 404);

        await access.DemandDepartmentAsync(current.CompanyId, current.BranchId, id, "DBRT03.UPDATE", cancellationToken);
        if (nextDivisionId != current.DivisionId)
        {
            var nextDivision = await db.Divisions.AsNoTracking()
                .Where(x => x.Id == nextDivisionId && x.CompanyId == current.CompanyId)
                .Select(x => new { x.BranchId })
                .SingleOrDefaultAsync(cancellationToken)
                ?? throw new RuleException("DIVISION_NOT_FOUND", "ไม่พบฝ่ายในบริษัทที่เลือก", 404);
            await access.DemandBranchAsync(current.CompanyId, nextDivision.BranchId, "DBRT03.UPDATE", cancellationToken);
        }

        return await transaction.ExecuteAsync(async () =>
        {
            var department = await db.LockDepartmentAsync(id, cancellationToken)
                ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลแผนก", 404);
            var oldDivisionId = department.DivisionId;
            var before = DepartmentSnapshot.Of(department);
            department.Update(
                nextDivisionId, request.Code, request.Name, IdValue.Parse(request.Version), actor.UserId, clock.GetUtcNow());

            Division? division;
            if (nextDivisionId != oldDivisionId)
            {
                division = await db.LockDivisionAsync(nextDivisionId, cancellationToken);
                if (division is null || division.CompanyId != department.CompanyId)
                    throw new RuleException("DIVISION_NOT_FOUND", "ไม่พบฝ่ายในบริษัทที่เลือก", 404);
                if (!division.IsActive)
                    throw new RuleException("PARENT_INACTIVE", "กรุณาเปิดใช้งานฝ่ายก่อน");
            }
            else
            {
                division = await db.Divisions.AsNoTracking().SingleAsync(x => x.Id == department.DivisionId, cancellationToken);
            }

            var branch = await db.Branches.AsNoTracking().SingleAsync(x => x.Id == division.BranchId, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            await audit.RecordOrganizationAsync(
                "DBRT03", "UPDATE", department.CompanyId, department.Id, before, DepartmentSnapshot.Of(department), cancellationToken);
            return DepartmentDto.Of(department, branch.Id, branch.Code, division.Code, division.Name);
        }, cancellationToken);
    }
}

public sealed record ChangeDepartmentStatusRequest(string Id, string Version, bool IsActive) : IRequest<DepartmentDto>;

public sealed class ChangeDepartmentStatusValidator : AbstractValidator<ChangeDepartmentStatusRequest>
{
    public ChangeDepartmentStatusValidator()
    {
        RuleFor(x => x.Id).ValidId();
        RuleFor(x => x.Version).ValidId("กรุณาโหลดข้อมูลล่าสุดก่อนบันทึก");
    }
}

public sealed class ChangeDepartmentStatusHandler(
    IOrganizationDbContext db,
    IAccessService access,
    ICurrentActor actor,
    IAtomicWriter transaction,
    IAuditWriter audit,
    TimeProvider clock) : IRequestHandler<ChangeDepartmentStatusRequest, DepartmentDto>
{
    public async Task<DepartmentDto> Handle(ChangeDepartmentStatusRequest request, CancellationToken cancellationToken)
    {
        var id = IdValue.Parse(request.Id);
        var action = request.IsActive ? "ACTIVATE" : "DEACTIVATE";
        var current = await (from department in db.Departments.AsNoTracking()
                             join division in db.Divisions.AsNoTracking() on department.DivisionId equals division.Id
                             where department.Id == id
                             select new { department.CompanyId, division.BranchId })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลแผนก", 404);
        await access.DemandDepartmentAsync(current.CompanyId, current.BranchId, id, "DBRT03." + action, cancellationToken);

        return await transaction.ExecuteAsync(async () =>
        {
            var department = await db.LockDepartmentAsync(id, cancellationToken)
                ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลแผนก", 404);
            var before = DepartmentSnapshot.Of(department);
            var division = await db.Divisions.AsNoTracking().SingleAsync(x => x.Id == department.DivisionId, cancellationToken);
            var branch = await db.Branches.AsNoTracking().SingleAsync(x => x.Id == division.BranchId, cancellationToken);
            var assignments = await access.HasActiveDepartmentAssignmentsAsync(department.CompanyId, id, cancellationToken);
            department.ChangeStatus(
                request.IsActive,
                IdValue.Parse(request.Version),
                division.IsActive,
                assignments,
                actor.UserId,
                clock.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);
            await audit.RecordOrganizationAsync(
                "DBRT03", action, department.CompanyId, department.Id, before, DepartmentSnapshot.Of(department), cancellationToken);
            return DepartmentDto.Of(department, branch.Id, branch.Code, division.Code, division.Name);
        }, cancellationToken);
    }
}

