using clean.Application.Common.Behaviors;
using clean.Application.Common.Interfaces;
using clean.Application.Common.Models;
using clean.Application.Features.DB.Shared;
using clean.Domain.Entities.Organization;
using clean.Domain.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace clean.Application.Features.DB.DBRT07;

public sealed record SearchBusinessUnitsRequest(
    string? Keyword = null,
    bool? IsActive = null,
    int PageNumber = 1,
    int PageSize = 25,
    string SortField = "code",
    string SortDirection = "asc") : IRequest<Page<BusinessUnitDto>>;

public sealed class SearchBusinessUnitsValidator : AbstractValidator<SearchBusinessUnitsRequest>
{
    public SearchBusinessUnitsValidator() => OrganizationMasterValidation.ApplyPagingRules(
        this, x => x.Keyword, x => x.PageNumber, x => x.PageSize, x => x.SortField, x => x.SortDirection,
        "code", "name", "isActive");
}

public sealed class SearchBusinessUnitsHandler(IOrganizationDbContext db, IAccessService access)
    : IRequestHandler<SearchBusinessUnitsRequest, Page<BusinessUnitDto>>
{
    public async Task<Page<BusinessUnitDto>> Handle(SearchBusinessUnitsRequest request, CancellationToken cancellationToken)
    {
        await access.DemandSharedViewAsync("DBRT07.VIEW", cancellationToken);
        var query = db.BusinessUnits.AsNoTracking().AsQueryable();
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
        return new(rows.Select(BusinessUnitDto.Of).ToArray(), count, request.PageNumber, request.PageSize);
    }
}

public sealed record GetBusinessUnitDetailRequest(string Id) : IRequest<BusinessUnitDto>;

public sealed class GetBusinessUnitDetailValidator : AbstractValidator<GetBusinessUnitDetailRequest>
{
    public GetBusinessUnitDetailValidator() => RuleFor(x => x.Id).ValidId();
}

public sealed class GetBusinessUnitDetailHandler(IOrganizationDbContext db, IAccessService access)
    : IRequestHandler<GetBusinessUnitDetailRequest, BusinessUnitDto>
{
    public async Task<BusinessUnitDto> Handle(GetBusinessUnitDetailRequest request, CancellationToken cancellationToken)
    {
        await access.DemandSharedViewAsync("DBRT07.VIEW", cancellationToken);
        var id = IdValue.Parse(request.Id);
        var unit = await db.BusinessUnits.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลหน่วยธุรกิจ", 404);
        return BusinessUnitDto.Of(unit);
    }
}

public sealed class CreateBusinessUnitRequest : IRequest<BusinessUnitDto>, INormalizableRequest
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public void Normalize() => Code = Company.NormalizeCode(Code);
}

public sealed class CreateBusinessUnitValidator : AbstractValidator<CreateBusinessUnitRequest>
{
    public CreateBusinessUnitValidator()
    {
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).ValidName();
    }
}

public sealed class CreateBusinessUnitHandler(
    IOrganizationDbContext db,
    IAccessService access,
    ICurrentActor actor,
    IAtomicWriter transaction,
    IAuditWriter audit,
    TimeProvider clock) : IRequestHandler<CreateBusinessUnitRequest, BusinessUnitDto>
{
    public async Task<BusinessUnitDto> Handle(CreateBusinessUnitRequest request, CancellationToken cancellationToken)
    {
        await access.DemandGlobalAsync("DBRT07.CREATE", cancellationToken);
        return await transaction.ExecuteAsync(async () =>
        {
            var unit = BusinessUnit.Create(request.Code, request.Name, actor.UserId, clock.GetUtcNow());
            db.BusinessUnits.Add(unit);
            await db.SaveChangesAsync(cancellationToken);
            await audit.RecordOrganizationAsync(
                "DBRT07", "CREATE", null, unit.Id, null, BusinessUnitSnapshot.Of(unit), cancellationToken);
            return BusinessUnitDto.Of(unit);
        }, cancellationToken);
    }
}

public sealed class UpdateBusinessUnitRequest : IRequest<BusinessUnitDto>, INormalizableRequest
{
    public string Id { get; set; } = "";
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public void Normalize() => Code = Company.NormalizeCode(Code);
}

public sealed class UpdateBusinessUnitValidator : AbstractValidator<UpdateBusinessUnitRequest>
{
    public UpdateBusinessUnitValidator()
    {
        RuleFor(x => x.Id).ValidId();
        RuleFor(x => x.Version).ValidId("กรุณาโหลดข้อมูลล่าสุดก่อนบันทึก");
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).ValidName();
    }
}

public sealed class UpdateBusinessUnitHandler(
    IOrganizationDbContext db,
    IAccessService access,
    ICurrentActor actor,
    IAtomicWriter transaction,
    IAuditWriter audit,
    TimeProvider clock) : IRequestHandler<UpdateBusinessUnitRequest, BusinessUnitDto>
{
    public async Task<BusinessUnitDto> Handle(UpdateBusinessUnitRequest request, CancellationToken cancellationToken)
    {
        await access.DemandGlobalAsync("DBRT07.UPDATE", cancellationToken);
        var id = IdValue.Parse(request.Id);
        return await transaction.ExecuteAsync(async () =>
        {
            var unit = await db.LockBusinessUnitAsync(id, cancellationToken)
                ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลหน่วยธุรกิจ", 404);
            var before = BusinessUnitSnapshot.Of(unit);
            unit.Update(request.Code, request.Name, IdValue.Parse(request.Version), actor.UserId, clock.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);
            await audit.RecordOrganizationAsync(
                "DBRT07", "UPDATE", null, unit.Id, before, BusinessUnitSnapshot.Of(unit), cancellationToken);
            return BusinessUnitDto.Of(unit);
        }, cancellationToken);
    }
}

public sealed record ChangeBusinessUnitStatusRequest(string Id, string Version, bool IsActive) : IRequest<BusinessUnitDto>;

public sealed class ChangeBusinessUnitStatusValidator : AbstractValidator<ChangeBusinessUnitStatusRequest>
{
    public ChangeBusinessUnitStatusValidator()
    {
        RuleFor(x => x.Id).ValidId();
        RuleFor(x => x.Version).ValidId("กรุณาโหลดข้อมูลล่าสุดก่อนบันทึก");
    }
}

public sealed class ChangeBusinessUnitStatusHandler(
    IOrganizationDbContext db,
    IAccessService access,
    ICurrentActor actor,
    IAtomicWriter transaction,
    IAuditWriter audit,
    TimeProvider clock) : IRequestHandler<ChangeBusinessUnitStatusRequest, BusinessUnitDto>
{
    public async Task<BusinessUnitDto> Handle(ChangeBusinessUnitStatusRequest request, CancellationToken cancellationToken)
    {
        var action = request.IsActive ? "ACTIVATE" : "DEACTIVATE";
        await access.DemandGlobalAsync("DBRT07." + action, cancellationToken);
        var id = IdValue.Parse(request.Id);
        return await transaction.ExecuteAsync(async () =>
        {
            var unit = await db.LockBusinessUnitAsync(id, cancellationToken)
                ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลหน่วยธุรกิจ", 404);
            var before = BusinessUnitSnapshot.Of(unit);
            unit.ChangeStatus(request.IsActive, IdValue.Parse(request.Version), actor.UserId, clock.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);
            await audit.RecordOrganizationAsync(
                "DBRT07", action, null, unit.Id, before, BusinessUnitSnapshot.Of(unit), cancellationToken);
            return BusinessUnitDto.Of(unit);
        }, cancellationToken);
    }
}

