using clean.Application.Common.Behaviors;
using clean.Application.Common.Interfaces;
using clean.Application.Common.Models;
using clean.Application.Features.DB.Shared;
using clean.Domain.Entities.Organization;
using clean.Domain.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace clean.Application.Features.DB.DBRT05;

public sealed record SearchPositionsRequest(
    string? Keyword = null,
    bool? IsActive = null,
    int PageNumber = 1,
    int PageSize = 25,
    string SortField = "code",
    string SortDirection = "asc") : IRequest<Page<PositionDto>>;

public sealed class SearchPositionsValidator : AbstractValidator<SearchPositionsRequest>
{
    public SearchPositionsValidator() => OrganizationMasterValidation.ApplyPagingRules(
        this, x => x.Keyword, x => x.PageNumber, x => x.PageSize, x => x.SortField, x => x.SortDirection,
        "code", "name", "isActive");
}

public sealed class SearchPositionsHandler(IOrganizationDbContext db, IAccessService access)
    : IRequestHandler<SearchPositionsRequest, Page<PositionDto>>
{
    public async Task<Page<PositionDto>> Handle(SearchPositionsRequest request, CancellationToken cancellationToken)
    {
        await access.DemandSharedViewAsync("DBRT05.VIEW", cancellationToken);
        var query = db.Positions.AsNoTracking().AsQueryable();
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
        return new(rows.Select(PositionDto.Of).ToArray(), count, request.PageNumber, request.PageSize);
    }
}

public sealed record GetPositionDetailRequest(string Id) : IRequest<PositionDto>;

public sealed class GetPositionDetailValidator : AbstractValidator<GetPositionDetailRequest>
{
    public GetPositionDetailValidator() => RuleFor(x => x.Id).ValidId();
}

public sealed class GetPositionDetailHandler(IOrganizationDbContext db, IAccessService access)
    : IRequestHandler<GetPositionDetailRequest, PositionDto>
{
    public async Task<PositionDto> Handle(GetPositionDetailRequest request, CancellationToken cancellationToken)
    {
        await access.DemandSharedViewAsync("DBRT05.VIEW", cancellationToken);
        var id = IdValue.Parse(request.Id);
        var position = await db.Positions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลตำแหน่งงาน", 404);
        return PositionDto.Of(position);
    }
}

public sealed class CreatePositionRequest : IRequest<PositionDto>, INormalizableRequest
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public void Normalize() => Code = Company.NormalizeCode(Code);
}

public sealed class CreatePositionValidator : AbstractValidator<CreatePositionRequest>
{
    public CreatePositionValidator()
    {
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).ValidName();
    }
}

public sealed class CreatePositionHandler(
    IOrganizationDbContext db,
    IAccessService access,
    ICurrentActor actor,
    IAtomicWriter transaction,
    IAuditWriter audit,
    TimeProvider clock) : IRequestHandler<CreatePositionRequest, PositionDto>
{
    public async Task<PositionDto> Handle(CreatePositionRequest request, CancellationToken cancellationToken)
    {
        await access.DemandGlobalAsync("DBRT05.CREATE", cancellationToken);
        return await transaction.ExecuteAsync(async () =>
        {
            var position = Position.Create(request.Code, request.Name, actor.UserId, clock.GetUtcNow());
            db.Positions.Add(position);
            await db.SaveChangesAsync(cancellationToken);
            await audit.RecordOrganizationAsync(
                "DBRT05", "CREATE", null, position.Id, null, PositionSnapshot.Of(position), cancellationToken);
            return PositionDto.Of(position);
        }, cancellationToken);
    }
}

public sealed class UpdatePositionRequest : IRequest<PositionDto>, INormalizableRequest
{
    public string Id { get; set; } = "";
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public void Normalize() => Code = Company.NormalizeCode(Code);
}

public sealed class UpdatePositionValidator : AbstractValidator<UpdatePositionRequest>
{
    public UpdatePositionValidator()
    {
        RuleFor(x => x.Id).ValidId();
        RuleFor(x => x.Version).ValidId("กรุณาโหลดข้อมูลล่าสุดก่อนบันทึก");
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).ValidName();
    }
}

public sealed class UpdatePositionHandler(
    IOrganizationDbContext db,
    IAccessService access,
    ICurrentActor actor,
    IAtomicWriter transaction,
    IAuditWriter audit,
    TimeProvider clock) : IRequestHandler<UpdatePositionRequest, PositionDto>
{
    public async Task<PositionDto> Handle(UpdatePositionRequest request, CancellationToken cancellationToken)
    {
        await access.DemandGlobalAsync("DBRT05.UPDATE", cancellationToken);
        var id = IdValue.Parse(request.Id);
        return await transaction.ExecuteAsync(async () =>
        {
            var position = await db.LockPositionAsync(id, cancellationToken)
                ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลตำแหน่งงาน", 404);
            var before = PositionSnapshot.Of(position);
            position.Update(request.Code, request.Name, IdValue.Parse(request.Version), actor.UserId, clock.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);
            await audit.RecordOrganizationAsync(
                "DBRT05", "UPDATE", null, position.Id, before, PositionSnapshot.Of(position), cancellationToken);
            return PositionDto.Of(position);
        }, cancellationToken);
    }
}

public sealed record ChangePositionStatusRequest(string Id, string Version, bool IsActive) : IRequest<PositionDto>;

public sealed class ChangePositionStatusValidator : AbstractValidator<ChangePositionStatusRequest>
{
    public ChangePositionStatusValidator()
    {
        RuleFor(x => x.Id).ValidId();
        RuleFor(x => x.Version).ValidId("กรุณาโหลดข้อมูลล่าสุดก่อนบันทึก");
    }
}

public sealed class ChangePositionStatusHandler(
    IOrganizationDbContext db,
    IAccessService access,
    ICurrentActor actor,
    IAtomicWriter transaction,
    IAuditWriter audit,
    TimeProvider clock) : IRequestHandler<ChangePositionStatusRequest, PositionDto>
{
    public async Task<PositionDto> Handle(ChangePositionStatusRequest request, CancellationToken cancellationToken)
    {
        var action = request.IsActive ? "ACTIVATE" : "DEACTIVATE";
        await access.DemandGlobalAsync("DBRT05." + action, cancellationToken);
        var id = IdValue.Parse(request.Id);
        return await transaction.ExecuteAsync(async () =>
        {
            var position = await db.LockPositionAsync(id, cancellationToken)
                ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลตำแหน่งงาน", 404);
            var before = PositionSnapshot.Of(position);
            position.ChangeStatus(request.IsActive, IdValue.Parse(request.Version), actor.UserId, clock.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);
            await audit.RecordOrganizationAsync(
                "DBRT05", action, null, position.Id, before, PositionSnapshot.Of(position), cancellationToken);
            return PositionDto.Of(position);
        }, cancellationToken);
    }
}

