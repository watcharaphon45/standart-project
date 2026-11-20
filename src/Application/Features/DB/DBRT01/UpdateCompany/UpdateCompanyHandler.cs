using clean.Application.Common.Interfaces;
using clean.Application.Common.Models;
using clean.Domain.Exceptions;
using MediatR;

namespace clean.Application.Features.DB.DBRT01.UpdateCompany;

public sealed class UpdateCompanyHandler(IOrganizationDbContext db, IAccessService access,
    ICurrentActor actor, IAtomicWriter transaction, IAuditWriter audit, TimeProvider clock)
    : IRequestHandler<UpdateCompanyRequest, CompanyDto>
{
    public async Task<CompanyDto> Handle(UpdateCompanyRequest request, CancellationToken cancellationToken)
    {
        var id = IdValue.Parse(request.Id);
        await access.DemandCompanyAsync(id, "DBRT01.UPDATE", cancellationToken);
        return await transaction.ExecuteAsync(async () =>
        {
            var company = await db.LockCompanyAsync(id, cancellationToken)
                ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลบริษัท", 404);
            var before = CompanySnapshot.Of(company);
            company.Update(request.Code, request.Name, IdValue.Parse(request.Version), actor.UserId, clock.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);
            await audit.CompanyChangedAsync("UPDATE", before, company, cancellationToken);
            return CompanyDto.Of(company);
        }, cancellationToken);
    }
}
