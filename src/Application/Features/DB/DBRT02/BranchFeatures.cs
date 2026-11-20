using clean.Application.Common.Behaviors;
using clean.Application.Common.Interfaces;
using clean.Application.Common.Models;
using clean.Application.Features.DB.Shared;
using clean.Domain.Entities.Organization;
using clean.Domain.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace clean.Application.Features.DB.DBRT02;

public sealed record SearchBranchesRequest(
    string CompanyId,
    string? Keyword = null,
    bool? IsActive = null,
    int PageNumber = 1,
    int PageSize = 25,
    string SortField = "code",
    string SortDirection = "asc") : IRequest<Page<BranchDto>>;

public sealed class SearchBranchesValidator : AbstractValidator<SearchBranchesRequest>
{
    public SearchBranchesValidator()
    {
        RuleFor(x => x.CompanyId).ValidId("กรุณาเลือกบริษัท");
        OrganizationMasterValidation.ApplyPagingRules(
            this,
            x => x.Keyword,
            x => x.PageNumber,
            x => x.PageSize,
            x => x.SortField,
            x => x.SortDirection,
            "code", "name", "isActive");
    }
}

public sealed class SearchBranchesHandler(IOrganizationDbContext db, IAccessService access)
    : IRequestHandler<SearchBranchesRequest, Page<BranchDto>>
{
    public async Task<Page<BranchDto>> Handle(SearchBranchesRequest request, CancellationToken cancellationToken)
    {
        var companyId = IdValue.Parse(request.CompanyId);
        var scope = await access.GetScopeFilterAsync(companyId, "DBRT02.VIEW", cancellationToken);
        var query = db.Branches.AsNoTracking().Where(x => x.CompanyId == companyId);
        if (!scope.FullCompany)
            query = query.Where(x => scope.BranchIds.Contains(x.Id));
        if (request.IsActive is not null)
            query = query.Where(x => x.IsActive == request.IsActive);
        var keyword = request.Keyword?.Trim();
        if (!string.IsNullOrEmpty(keyword))
        {
            var upper = keyword.ToUpperInvariant();
            query = query.Where(x => x.Code.Contains(upper) || x.Name.ToUpper().Contains(upper));
        }

        var count = await query.CountAsync(cancellationToken);
        var ascending = request.SortDirection == "asc";
        var sorted = (request.SortField, ascending) switch
        {
            ("name", true) => query.OrderBy(x => x.Name),
            ("name", false) => query.OrderByDescending(x => x.Name),
            ("isActive", true) => query.OrderBy(x => x.IsActive),
            ("isActive", false) => query.OrderByDescending(x => x.IsActive),
            (_, true) => query.OrderBy(x => x.Code),
            _ => query.OrderByDescending(x => x.Code)
        };
        var rows = await sorted.ThenBy(x => x.Id)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);
        return new(rows.Select(BranchDto.Of).ToArray(), count, request.PageNumber, request.PageSize);
    }
}

public sealed record GetBranchDetailRequest(string Id) : IRequest<BranchDto>;

public sealed class GetBranchDetailValidator : AbstractValidator<GetBranchDetailRequest>
{
    public GetBranchDetailValidator() => RuleFor(x => x.Id).ValidId();
}

public sealed class GetBranchDetailHandler(IOrganizationDbContext db, IAccessService access)
    : IRequestHandler<GetBranchDetailRequest, BranchDto>
{
    public async Task<BranchDto> Handle(GetBranchDetailRequest request, CancellationToken cancellationToken)
    {
        var id = IdValue.Parse(request.Id);
        var branch = await db.Branches.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลสาขา", 404);
        await access.DemandBranchAsync(branch.CompanyId, branch.Id, "DBRT02.VIEW", cancellationToken);
        return BranchDto.Of(branch);
    }
}

public sealed class CreateBranchRequest : IRequest<BranchDto>, INormalizableRequest
{
    public string CompanyId { get; set; } = "";
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public void Normalize() => Code = Company.NormalizeCode(Code);
}

public sealed class CreateBranchValidator : AbstractValidator<CreateBranchRequest>
{
    public CreateBranchValidator()
    {
        RuleFor(x => x.CompanyId).ValidId("กรุณาเลือกบริษัท");
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).ValidName();
    }
}

public sealed class CreateBranchHandler(
    IOrganizationDbContext db,
    IAccessService access,
    ICurrentActor actor,
    IAtomicWriter transaction,
    IAuditWriter audit,
    TimeProvider clock) : IRequestHandler<CreateBranchRequest, BranchDto>
{
    public async Task<BranchDto> Handle(CreateBranchRequest request, CancellationToken cancellationToken)
    {
        var companyId = IdValue.Parse(request.CompanyId);
        await access.DemandCompanyAsync(companyId, "DBRT02.CREATE", cancellationToken);
        return await transaction.ExecuteAsync(async () =>
        {
            var company = await db.LockCompanyAsync(companyId, cancellationToken)
                ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลบริษัท", 404);
            if (!company.IsActive)
                throw new RuleException("PARENT_INACTIVE", "กรุณาเปิดใช้งานบริษัทก่อน");
            var branch = Branch.Create(companyId, request.Code, request.Name, actor.UserId, clock.GetUtcNow());
            db.Branches.Add(branch);
            await db.SaveChangesAsync(cancellationToken);
            await audit.RecordOrganizationAsync(
                "DBRT02", "CREATE", branch.CompanyId, branch.Id, null, BranchSnapshot.Of(branch), cancellationToken);
            return BranchDto.Of(branch);
        }, cancellationToken);
    }
}

public sealed class UpdateBranchRequest : IRequest<BranchDto>, INormalizableRequest
{
    public string Id { get; set; } = "";
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public void Normalize() => Code = Company.NormalizeCode(Code);
}

public sealed class UpdateBranchValidator : AbstractValidator<UpdateBranchRequest>
{
    public UpdateBranchValidator()
    {
        RuleFor(x => x.Id).ValidId();
        RuleFor(x => x.Version).ValidId("กรุณาโหลดข้อมูลล่าสุดก่อนบันทึก");
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).ValidName();
    }
}

public sealed class UpdateBranchHandler(
    IOrganizationDbContext db,
    IAccessService access,
    ICurrentActor actor,
    IAtomicWriter transaction,
    IAuditWriter audit,
    TimeProvider clock) : IRequestHandler<UpdateBranchRequest, BranchDto>
{
    public async Task<BranchDto> Handle(UpdateBranchRequest request, CancellationToken cancellationToken)
    {
        var id = IdValue.Parse(request.Id);
        var target = await db.Branches.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.CompanyId })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลสาขา", 404);
        await access.DemandBranchAsync(target.CompanyId, id, "DBRT02.UPDATE", cancellationToken);

        return await transaction.ExecuteAsync(async () =>
        {
            var branch = await db.LockBranchAsync(id, cancellationToken)
                ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลสาขา", 404);
            var before = BranchSnapshot.Of(branch);
            branch.Update(request.Code, request.Name, IdValue.Parse(request.Version), actor.UserId, clock.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);
            await audit.RecordOrganizationAsync(
                "DBRT02", "UPDATE", branch.CompanyId, branch.Id, before, BranchSnapshot.Of(branch), cancellationToken);
            return BranchDto.Of(branch);
        }, cancellationToken);
    }
}

public sealed record ChangeBranchStatusRequest(string Id, string Version, bool IsActive) : IRequest<BranchDto>;

public sealed class ChangeBranchStatusValidator : AbstractValidator<ChangeBranchStatusRequest>
{
    public ChangeBranchStatusValidator()
    {
        RuleFor(x => x.Id).ValidId();
        RuleFor(x => x.Version).ValidId("กรุณาโหลดข้อมูลล่าสุดก่อนบันทึก");
    }
}

public sealed class ChangeBranchStatusHandler(
    IOrganizationDbContext db,
    IAccessService access,
    ICurrentActor actor,
    IAtomicWriter transaction,
    IAuditWriter audit,
    TimeProvider clock) : IRequestHandler<ChangeBranchStatusRequest, BranchDto>
{
    public async Task<BranchDto> Handle(ChangeBranchStatusRequest request, CancellationToken cancellationToken)
    {
        var id = IdValue.Parse(request.Id);
        var action = request.IsActive ? "ACTIVATE" : "DEACTIVATE";
        var target = await db.Branches.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.CompanyId })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลสาขา", 404);
        await access.DemandBranchAsync(target.CompanyId, id, "DBRT02." + action, cancellationToken);

        return await transaction.ExecuteAsync(async () =>
        {
            var branch = await db.LockBranchAsync(id, cancellationToken)
                ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลสาขา", 404);
            var before = BranchSnapshot.Of(branch);
            var parentActive = await db.Companies.AsNoTracking()
                .Where(x => x.Id == branch.CompanyId)
                .Select(x => x.IsActive)
                .SingleAsync(cancellationToken);
            var children = await db.Divisions.AnyAsync(x => x.BranchId == id && x.IsActive, cancellationToken);
            var assignments = await access.HasActiveBranchAssignmentsAsync(branch.CompanyId, id, cancellationToken);
            branch.ChangeStatus(
                request.IsActive,
                IdValue.Parse(request.Version),
                parentActive,
                children,
                assignments,
                actor.UserId,
                clock.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);
            await audit.RecordOrganizationAsync(
                "DBRT02", action, branch.CompanyId, branch.Id, before, BranchSnapshot.Of(branch), cancellationToken);
            return BranchDto.Of(branch);
        }, cancellationToken);
    }
}

