using clean.Application.Common.Interfaces;
using clean.Application.Common.Models;
using clean.Domain.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace clean.Application.Features.DB.DBRT01.GetCompanyDetail;

public sealed record GetCompanyDetailRequest(string Id) : IRequest<CompanyDto>;
public sealed class GetCompanyDetailValidator : AbstractValidator<GetCompanyDetailRequest>
{
    public GetCompanyDetailValidator() => RuleFor(x => x.Id).Must(IdValue.IsValid).WithMessage("รหัสอ้างอิงไม่ถูกต้อง");
}
public sealed class GetCompanyDetailHandler(IOrganizationDbContext db, IAccessService access)
    : IRequestHandler<GetCompanyDetailRequest, CompanyDto>
{
    public async Task<CompanyDto> Handle(GetCompanyDetailRequest request, CancellationToken cancellationToken)
    {
        var id = IdValue.Parse(request.Id);
        await access.DemandCompanyAsync(id, "DBRT01.VIEW", cancellationToken);
        var company = await db.Companies.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลบริษัท", 404);
        return CompanyDto.Of(company);
    }
}
