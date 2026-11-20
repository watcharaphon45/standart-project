using clean.Application.Common.Interfaces;
using clean.Application.Common.Models;
using clean.Domain.Entities.Organization;
using MediatR;

namespace clean.Application.Features.DB.DBRT01.CreateCompany;

public sealed class CreateCompanyHandler(IOrganizationDbContext db, IAccessService access,
    ICurrentActor actor, IAtomicWriter transaction, IAuditWriter audit, TimeProvider clock)
    : IRequestHandler<CreateCompanyRequest, CompanyDto>
{
    public async Task<CompanyDto> Handle(CreateCompanyRequest request, CancellationToken cancellationToken)
    {
        // Creating a new authorization boundary requires an explicit global grant.
        await access.DemandGlobalAsync("DBRT01.CREATE", cancellationToken);
        return await transaction.ExecuteAsync(async () =>
        {
            var company = Company.Create(request.Code, request.Name, actor.UserId, clock.GetUtcNow());
            db.Companies.Add(company);
            await db.SaveChangesAsync(cancellationToken);
            await audit.CompanyChangedAsync("CREATE", null, company, cancellationToken);
            return CompanyDto.Of(company);
        }, cancellationToken);
    }
}
