using clean.Application.Common.Interfaces;
using clean.Application.Common.Models;
using clean.Domain.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace clean.Application.Features.DB.DBRT01.ChangeCompanyStatus;

public sealed record ChangeCompanyStatusRequest(string Id, string Version, bool IsActive) : IRequest<CompanyDto>;
public sealed class ChangeCompanyStatusValidator : AbstractValidator<ChangeCompanyStatusRequest>
{
    public ChangeCompanyStatusValidator()
    {
        RuleFor(x => x.Id).Must(IdValue.IsValid).WithMessage("รหัสอ้างอิงไม่ถูกต้อง");
        RuleFor(x => x.Version).Must(IdValue.IsValid).WithMessage("กรุณาโหลดข้อมูลล่าสุดก่อนบันทึก");
    }
}
public sealed class ChangeCompanyStatusHandler(IOrganizationDbContext db, IAccessService access,
    ICurrentActor actor, IAtomicWriter transaction, IAuditWriter audit, TimeProvider clock)
    : IRequestHandler<ChangeCompanyStatusRequest, CompanyDto>
{
    public async Task<CompanyDto> Handle(ChangeCompanyStatusRequest request, CancellationToken cancellationToken)
    {
        var id = IdValue.Parse(request.Id);
        var action = request.IsActive ? "ACTIVATE" : "DEACTIVATE";
        await access.DemandCompanyAsync(id, "DBRT01." + action, cancellationToken);
        return await transaction.ExecuteAsync(async () =>
        {
            var company = await db.LockCompanyAsync(id, cancellationToken)
                ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลบริษัท", 404);
            var before = CompanySnapshot.Of(company);
            var children = await db.Branches.AnyAsync(x => x.CompanyId == id && x.IsActive, cancellationToken)
                || await db.CostCenters.AnyAsync(x => x.CompanyId == id && x.IsActive, cancellationToken);
            var assignments = await access.HasActiveAssignmentsAsync(id, cancellationToken);
            company.ChangeStatus(request.IsActive, IdValue.Parse(request.Version), children, assignments, actor.UserId, clock.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);
            await audit.CompanyChangedAsync(action, before, company, cancellationToken);
            return CompanyDto.Of(company);
        }, cancellationToken);
    }
}
