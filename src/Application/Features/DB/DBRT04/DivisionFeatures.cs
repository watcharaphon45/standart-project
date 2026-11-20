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

namespace clean.Application.Features.DB.DBRT04;

public sealed record SearchDivisionsRequest(
    string CompanyId,
    string? BranchId = null,
    string? Keyword = null,
    bool? IsActive = null,
    int PageNumber = 1,
    int PageSize = 25,
    string SortField = "code",
    string SortDirection = "asc") : IRequest<Page<DivisionDto>>;

public sealed class SearchDivisionsValidator : AbstractValidator<SearchDivisionsRequest>
{
    public SearchDivisionsValidator()
    {
        RuleFor(x => x.CompanyId).ValidId("กรุณาเลือกบริษัท");
        RuleFor(x => x.BranchId).ValidOptionalId("รหัสสาขาไม่ถูกต้อง");
        OrganizationMasterValidation.ApplyPagingRules(
            this,
            x => x.Keyword,
            x => x.PageNumber,
            x => x.PageSize,
            x => x.SortField,
            x => x.SortDirection,
            "code", "name", "branchCode", "isActive");
    }
}

public sealed class SearchDivisionsHandler(IOrganizationDbContext db, IAccessService access)
    : IRequestHandler<SearchDivisionsRequest, Page<DivisionDto>>
{
    public async Task<Page<DivisionDto>> Handle(SearchDivisionsRequest request, CancellationToken cancellationToken)
    {
        var companyId = IdValue.Parse(request.CompanyId);
        var scope = await access.GetScopeFilterAsync(companyId, "DBRT04.VIEW", cancellationToken);
        var query = from division in db.Divisions.AsNoTracking()
                    join branch in db.Branches.AsNoTracking() on division.BranchId equals branch.Id
                    where division.CompanyId == companyId
                    select new { division, branch };

        if (!scope.FullCompany)
            query = query.Where(x => scope.BranchIds.Contains(x.division.BranchId));
        if (!string.IsNullOrEmpty(request.BranchId))
        {
            var branchId = IdValue.Parse(request.BranchId);
            query = query.Where(x => x.division.BranchId == branchId);
        }
        if (request.IsActive is not null)
            query = query.Where(x => x.division.IsActive == request.IsActive);
        var keyword = request.Keyword?.Trim();
        if (!string.IsNullOrEmpty(keyword))
        {
            var upper = keyword.ToUpperInvariant();
            query = query.Where(x =>
                x.division.Code.Contains(upper)
                || x.division.Name.ToUpper().Contains(upper)
                || x.branch.Code.Contains(upper)
                || x.branch.Name.ToUpper().Contains(upper));
        }

        var count = await query.CountAsync(cancellationToken);
        var ascending = request.SortDirection == "asc";
        var sorted = (request.SortField, ascending) switch
        {
            ("name", true) => query.OrderBy(x => x.division.Name),
            ("name", false) => query.OrderByDescending(x => x.division.Name),
            ("branchCode", true) => query.OrderBy(x => x.branch.Code),
            ("branchCode", false) => query.OrderByDescending(x => x.branch.Code),
            ("isActive", true) => query.OrderBy(x => x.division.IsActive),
            ("isActive", false) => query.OrderByDescending(x => x.division.IsActive),
            (_, true) => query.OrderBy(x => x.division.Code),
            _ => query.OrderByDescending(x => x.division.Code)
        };
        var rows = await sorted.ThenBy(x => x.division.Id)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new(
            rows.Select(x => DivisionDto.Of(x.division, x.branch.Code, x.branch.Name)).ToArray(),
            count,
            request.PageNumber,
            request.PageSize);
    }
}

public sealed record GetDivisionOptionsRequest(string CompanyId) : IRequest<IReadOnlyList<LookupOptionDto>>;

public sealed class GetDivisionOptionsValidator : AbstractValidator<GetDivisionOptionsRequest>
{
    public GetDivisionOptionsValidator() => RuleFor(x => x.CompanyId).ValidId("กรุณาเลือกบริษัท");
}

public sealed class GetDivisionOptionsHandler(IOrganizationDbContext db, IAccessService access)
    : IRequestHandler<GetDivisionOptionsRequest, IReadOnlyList<LookupOptionDto>>
{
    public async Task<IReadOnlyList<LookupOptionDto>> Handle(GetDivisionOptionsRequest request, CancellationToken cancellationToken)
    {
        var companyId = IdValue.Parse(request.CompanyId);
        var scope = await access.GetScopeFilterAnyAsync(
            companyId,
            ["DBRT04.VIEW", "DBRT04.CREATE", "DBRT04.UPDATE"],
            cancellationToken);
        var query = db.Branches.AsNoTracking().Where(x => x.CompanyId == companyId);
        if (!scope.FullCompany)
            query = query.Where(x => scope.BranchIds.Contains(x.Id));
        var branches = await query.OrderBy(x => x.Code).ToListAsync(cancellationToken);
        return branches.Select(x => new LookupOptionDto(
            x.Id.ToString(CultureInfo.InvariantCulture),
            x.Code,
            x.Name,
            x.IsActive)).ToArray();
    }
}

public sealed record GetDivisionDetailRequest(string Id) : IRequest<DivisionDto>;

public sealed class GetDivisionDetailValidator : AbstractValidator<GetDivisionDetailRequest>
{
    public GetDivisionDetailValidator() => RuleFor(x => x.Id).ValidId();
}

public sealed class GetDivisionDetailHandler(IOrganizationDbContext db, IAccessService access)
    : IRequestHandler<GetDivisionDetailRequest, DivisionDto>
{
    public async Task<DivisionDto> Handle(GetDivisionDetailRequest request, CancellationToken cancellationToken)
    {
        var id = IdValue.Parse(request.Id);
        var row = await (from division in db.Divisions.AsNoTracking()
                         join branch in db.Branches.AsNoTracking() on division.BranchId equals branch.Id
                         where division.Id == id
                         select new { division, branch }).SingleOrDefaultAsync(cancellationToken)
            ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลฝ่าย", 404);
        await access.DemandBranchAsync(row.division.CompanyId, row.division.BranchId, "DBRT04.VIEW", cancellationToken);
        return DivisionDto.Of(row.division, row.branch.Code, row.branch.Name);
    }
}

public sealed class CreateDivisionRequest : IRequest<DivisionDto>, INormalizableRequest
{
    public string CompanyId { get; set; } = "";
    public string BranchId { get; set; } = "";
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public void Normalize() => Code = Company.NormalizeCode(Code);
}

public sealed class CreateDivisionValidator : AbstractValidator<CreateDivisionRequest>
{
    public CreateDivisionValidator()
    {
        RuleFor(x => x.CompanyId).ValidId("กรุณาเลือกบริษัท");
        RuleFor(x => x.BranchId).ValidId("กรุณาเลือกสาขา");
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).ValidName();
    }
}

public sealed class CreateDivisionHandler(
    IOrganizationDbContext db,
    IAccessService access,
    ICurrentActor actor,
    IAtomicWriter transaction,
    IAuditWriter audit,
    TimeProvider clock) : IRequestHandler<CreateDivisionRequest, DivisionDto>
{
    public async Task<DivisionDto> Handle(CreateDivisionRequest request, CancellationToken cancellationToken)
    {
        var companyId = IdValue.Parse(request.CompanyId);
        var branchId = IdValue.Parse(request.BranchId);
        await access.DemandBranchAsync(companyId, branchId, "DBRT04.CREATE", cancellationToken);

        return await transaction.ExecuteAsync(async () =>
        {
            var branch = await db.LockBranchAsync(branchId, cancellationToken);
            if (branch is null || branch.CompanyId != companyId)
                throw new RuleException("BRANCH_NOT_FOUND", "ไม่พบสาขาในบริษัทที่เลือก", 404);
            if (!branch.IsActive)
                throw new RuleException("PARENT_INACTIVE", "กรุณาเปิดใช้งานสาขาก่อน");

            var division = Division.Create(companyId, branchId, request.Code, request.Name, actor.UserId, clock.GetUtcNow());
            db.Divisions.Add(division);
            await db.SaveChangesAsync(cancellationToken);
            await audit.RecordOrganizationAsync(
                "DBRT04", "CREATE", division.CompanyId, division.Id, null, DivisionSnapshot.Of(division), cancellationToken);
            return DivisionDto.Of(division, branch.Code, branch.Name);
        }, cancellationToken);
    }
}

public sealed class UpdateDivisionRequest : IRequest<DivisionDto>, INormalizableRequest
{
    public string Id { get; set; } = "";
    public string BranchId { get; set; } = "";
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public void Normalize() => Code = Company.NormalizeCode(Code);
}

public sealed class UpdateDivisionValidator : AbstractValidator<UpdateDivisionRequest>
{
    public UpdateDivisionValidator()
    {
        RuleFor(x => x.Id).ValidId();
        RuleFor(x => x.BranchId).ValidId("กรุณาเลือกสาขา");
        RuleFor(x => x.Version).ValidId("กรุณาโหลดข้อมูลล่าสุดก่อนบันทึก");
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).ValidName();
    }
}

public sealed class UpdateDivisionHandler(
    IOrganizationDbContext db,
    IAccessService access,
    ICurrentActor actor,
    IAtomicWriter transaction,
    IAuditWriter audit,
    TimeProvider clock) : IRequestHandler<UpdateDivisionRequest, DivisionDto>
{
    public async Task<DivisionDto> Handle(UpdateDivisionRequest request, CancellationToken cancellationToken)
    {
        var id = IdValue.Parse(request.Id);
        var nextBranchId = IdValue.Parse(request.BranchId);
        var target = await db.Divisions.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.CompanyId, x.BranchId })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลฝ่าย", 404);

        await access.DemandBranchAsync(target.CompanyId, target.BranchId, "DBRT04.UPDATE", cancellationToken);
        if (nextBranchId != target.BranchId)
            await access.DemandBranchAsync(target.CompanyId, nextBranchId, "DBRT04.UPDATE", cancellationToken);

        return await transaction.ExecuteAsync(async () =>
        {
            var division = await db.LockDivisionAsync(id, cancellationToken)
                ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลฝ่าย", 404);
            var oldBranchId = division.BranchId;
            var before = DivisionSnapshot.Of(division);
            division.Update(nextBranchId, request.Code, request.Name, IdValue.Parse(request.Version), actor.UserId, clock.GetUtcNow());

            Branch? branch;
            if (nextBranchId != oldBranchId)
            {
                branch = await db.LockBranchAsync(nextBranchId, cancellationToken);
                if (branch is null || branch.CompanyId != division.CompanyId)
                    throw new RuleException("BRANCH_NOT_FOUND", "ไม่พบสาขาในบริษัทที่เลือก", 404);
                if (!branch.IsActive)
                    throw new RuleException("PARENT_INACTIVE", "กรุณาเปิดใช้งานสาขาก่อน");
            }
            else
            {
                branch = await db.Branches.AsNoTracking().SingleAsync(x => x.Id == division.BranchId, cancellationToken);
            }

            await db.SaveChangesAsync(cancellationToken);
            await audit.RecordOrganizationAsync(
                "DBRT04", "UPDATE", division.CompanyId, division.Id, before, DivisionSnapshot.Of(division), cancellationToken);
            return DivisionDto.Of(division, branch.Code, branch.Name);
        }, cancellationToken);
    }
}

public sealed record ChangeDivisionStatusRequest(string Id, string Version, bool IsActive) : IRequest<DivisionDto>;

public sealed class ChangeDivisionStatusValidator : AbstractValidator<ChangeDivisionStatusRequest>
{
    public ChangeDivisionStatusValidator()
    {
        RuleFor(x => x.Id).ValidId();
        RuleFor(x => x.Version).ValidId("กรุณาโหลดข้อมูลล่าสุดก่อนบันทึก");
    }
}

public sealed class ChangeDivisionStatusHandler(
    IOrganizationDbContext db,
    IAccessService access,
    ICurrentActor actor,
    IAtomicWriter transaction,
    IAuditWriter audit,
    TimeProvider clock) : IRequestHandler<ChangeDivisionStatusRequest, DivisionDto>
{
    public async Task<DivisionDto> Handle(ChangeDivisionStatusRequest request, CancellationToken cancellationToken)
    {
        var id = IdValue.Parse(request.Id);
        var action = request.IsActive ? "ACTIVATE" : "DEACTIVATE";
        var target = await db.Divisions.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.CompanyId, x.BranchId })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลฝ่าย", 404);
        await access.DemandBranchAsync(target.CompanyId, target.BranchId, "DBRT04." + action, cancellationToken);

        return await transaction.ExecuteAsync(async () =>
        {
            var division = await db.LockDivisionAsync(id, cancellationToken)
                ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลฝ่าย", 404);
            var before = DivisionSnapshot.Of(division);
            var branch = await db.Branches.AsNoTracking().SingleAsync(x => x.Id == division.BranchId, cancellationToken);
            var children = await db.Departments.AnyAsync(x => x.DivisionId == id && x.IsActive, cancellationToken);
            division.ChangeStatus(
                request.IsActive,
                IdValue.Parse(request.Version),
                branch.IsActive,
                children,
                actor.UserId,
                clock.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);
            await audit.RecordOrganizationAsync(
                "DBRT04", action, division.CompanyId, division.Id, before, DivisionSnapshot.Of(division), cancellationToken);
            return DivisionDto.Of(division, branch.Code, branch.Name);
        }, cancellationToken);
    }
}

