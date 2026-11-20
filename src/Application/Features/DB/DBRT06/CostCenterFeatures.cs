using clean.Application.Common.Behaviors;
using clean.Application.Common.Interfaces;
using clean.Application.Common.Models;
using clean.Application.Features.DB.Shared;
using clean.Domain.Entities.Organization;
using clean.Domain.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace clean.Application.Features.DB.DBRT06;

public sealed record SearchCostCentersRequest(
    string CompanyId,
    string? Keyword = null,
    bool? IsActive = null,
    int PageNumber = 1,
    int PageSize = 25,
    string SortField = "code",
    string SortDirection = "asc") : IRequest<Page<CostCenterDto>>;

public sealed class SearchCostCentersValidator : AbstractValidator<SearchCostCentersRequest>
{
    public SearchCostCentersValidator()
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

public sealed class SearchCostCentersHandler(IOrganizationDbContext db, IAccessService access)
    : IRequestHandler<SearchCostCentersRequest, Page<CostCenterDto>>
{
    public async Task<Page<CostCenterDto>> Handle(SearchCostCentersRequest request, CancellationToken cancellationToken)
    {
        var companyId = IdValue.Parse(request.CompanyId);
        await access.DemandCompanyAsync(companyId, "DBRT06.VIEW", cancellationToken);
        var query = db.CostCenters.AsNoTracking().Where(x => x.CompanyId == companyId);
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
        return new(rows.Select(CostCenterDto.Of).ToArray(), count, request.PageNumber, request.PageSize);
    }
}

public sealed record GetCostCenterDetailRequest(string Id) : IRequest<CostCenterDto>;

public sealed class GetCostCenterDetailValidator : AbstractValidator<GetCostCenterDetailRequest>
{
    public GetCostCenterDetailValidator() => RuleFor(x => x.Id).ValidId();
}

public sealed class GetCostCenterDetailHandler(IOrganizationDbContext db, IAccessService access)
    : IRequestHandler<GetCostCenterDetailRequest, CostCenterDto>
{
    public async Task<CostCenterDto> Handle(GetCostCenterDetailRequest request, CancellationToken cancellationToken)
    {
        var id = IdValue.Parse(request.Id);
        var costCenter = await db.CostCenters.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลศูนย์ต้นทุน", 404);
        await access.DemandCompanyAsync(costCenter.CompanyId, "DBRT06.VIEW", cancellationToken);
        return CostCenterDto.Of(costCenter);
    }
}

public sealed class CreateCostCenterRequest : IRequest<CostCenterDto>, INormalizableRequest
{
    public string CompanyId { get; set; } = "";
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public void Normalize() => Code = Company.NormalizeCode(Code);
}

public sealed class CreateCostCenterValidator : AbstractValidator<CreateCostCenterRequest>
{
    public CreateCostCenterValidator()
    {
        RuleFor(x => x.CompanyId).ValidId("กรุณาเลือกบริษัท");
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).ValidName();
    }
}

public sealed class CreateCostCenterHandler(
    IOrganizationDbContext db,
    IAccessService access,
    ICurrentActor actor,
    IAtomicWriter transaction,
    IAuditWriter audit,
    TimeProvider clock) : IRequestHandler<CreateCostCenterRequest, CostCenterDto>
{
    public async Task<CostCenterDto> Handle(CreateCostCenterRequest request, CancellationToken cancellationToken)
    {
        var companyId = IdValue.Parse(request.CompanyId);
        await access.DemandCompanyAsync(companyId, "DBRT06.CREATE", cancellationToken);

        return await transaction.ExecuteAsync(async () =>
        {
            var company = await db.LockCompanyAsync(companyId, cancellationToken)
                ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลบริษัท", 404);
            if (!company.IsActive)
                throw new RuleException("PARENT_INACTIVE", "กรุณาเปิดใช้งานบริษัทก่อน");

            var costCenter = CostCenter.Create(companyId, request.Code, request.Name, actor.UserId, clock.GetUtcNow());
            db.CostCenters.Add(costCenter);
            await db.SaveChangesAsync(cancellationToken);
            await audit.RecordOrganizationAsync(
                "DBRT06", "CREATE", costCenter.CompanyId, costCenter.Id, null, CostCenterSnapshot.Of(costCenter), cancellationToken);
            return CostCenterDto.Of(costCenter);
        }, cancellationToken);
    }
}

public sealed class UpdateCostCenterRequest : IRequest<CostCenterDto>, INormalizableRequest
{
    public string Id { get; set; } = "";
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public void Normalize() => Code = Company.NormalizeCode(Code);
}

public sealed class UpdateCostCenterValidator : AbstractValidator<UpdateCostCenterRequest>
{
    public UpdateCostCenterValidator()
    {
        RuleFor(x => x.Id).ValidId();
        RuleFor(x => x.Version).ValidId("กรุณาโหลดข้อมูลล่าสุดก่อนบันทึก");
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).ValidName();
    }
}

public sealed class UpdateCostCenterHandler(
    IOrganizationDbContext db,
    IAccessService access,
    ICurrentActor actor,
    IAtomicWriter transaction,
    IAuditWriter audit,
    TimeProvider clock) : IRequestHandler<UpdateCostCenterRequest, CostCenterDto>
{
    public async Task<CostCenterDto> Handle(UpdateCostCenterRequest request, CancellationToken cancellationToken)
    {
        var id = IdValue.Parse(request.Id);
        var target = await db.CostCenters.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.CompanyId })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลศูนย์ต้นทุน", 404);
        await access.DemandCompanyAsync(target.CompanyId, "DBRT06.UPDATE", cancellationToken);

        return await transaction.ExecuteAsync(async () =>
        {
            var costCenter = await db.LockCostCenterAsync(id, cancellationToken)
                ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลศูนย์ต้นทุน", 404);
            var before = CostCenterSnapshot.Of(costCenter);
            costCenter.Update(request.Code, request.Name, IdValue.Parse(request.Version), actor.UserId, clock.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);
            await audit.RecordOrganizationAsync(
                "DBRT06", "UPDATE", costCenter.CompanyId, costCenter.Id, before, CostCenterSnapshot.Of(costCenter), cancellationToken);
            return CostCenterDto.Of(costCenter);
        }, cancellationToken);
    }
}

public sealed record ChangeCostCenterStatusRequest(string Id, string Version, bool IsActive) : IRequest<CostCenterDto>;

public sealed class ChangeCostCenterStatusValidator : AbstractValidator<ChangeCostCenterStatusRequest>
{
    public ChangeCostCenterStatusValidator()
    {
        RuleFor(x => x.Id).ValidId();
        RuleFor(x => x.Version).ValidId("กรุณาโหลดข้อมูลล่าสุดก่อนบันทึก");
    }
}

public sealed class ChangeCostCenterStatusHandler(
    IOrganizationDbContext db,
    IAccessService access,
    ICurrentActor actor,
    IAtomicWriter transaction,
    IAuditWriter audit,
    TimeProvider clock) : IRequestHandler<ChangeCostCenterStatusRequest, CostCenterDto>
{
    public async Task<CostCenterDto> Handle(ChangeCostCenterStatusRequest request, CancellationToken cancellationToken)
    {
        var id = IdValue.Parse(request.Id);
        var action = request.IsActive ? "ACTIVATE" : "DEACTIVATE";
        var target = await db.CostCenters.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.CompanyId })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลศูนย์ต้นทุน", 404);
        await access.DemandCompanyAsync(target.CompanyId, "DBRT06." + action, cancellationToken);

        return await transaction.ExecuteAsync(async () =>
        {
            var costCenter = await db.LockCostCenterAsync(id, cancellationToken)
                ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลศูนย์ต้นทุน", 404);
            var before = CostCenterSnapshot.Of(costCenter);
            var parentActive = await db.Companies.AsNoTracking()
                .Where(x => x.Id == costCenter.CompanyId)
                .Select(x => x.IsActive)
                .SingleAsync(cancellationToken);
            costCenter.ChangeStatus(
                request.IsActive,
                IdValue.Parse(request.Version),
                parentActive,
                actor.UserId,
                clock.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);
            await audit.RecordOrganizationAsync(
                "DBRT06", action, costCenter.CompanyId, costCenter.Id, before, CostCenterSnapshot.Of(costCenter), cancellationToken);
            return CostCenterDto.Of(costCenter);
        }, cancellationToken);
    }
}

