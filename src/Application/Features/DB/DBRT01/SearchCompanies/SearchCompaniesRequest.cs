using clean.Application.Common.Interfaces;
using clean.Application.Common.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace clean.Application.Features.DB.DBRT01.SearchCompanies;

public sealed record SearchCompaniesRequest(string CompanyId, string? Keyword = null, bool? IsActive = null,
    int PageNumber = 1, int PageSize = 25, string SortField = "code", string SortDirection = "asc") : IRequest<Page<CompanyDto>>;

public sealed class SearchCompaniesValidator : AbstractValidator<SearchCompaniesRequest>
{
    public SearchCompaniesValidator()
    {
        RuleFor(x => x.CompanyId).Must(IdValue.IsValid).WithMessage("กรุณาเลือกบริษัท");
        RuleFor(x => x.Keyword).MaximumLength(200);
        RuleFor(x => x.PageNumber).InclusiveBetween(1, 1000000);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.SortField).Must(x => x is "code" or "name" or "isActive");
        RuleFor(x => x.SortDirection).Must(x => x is "asc" or "desc");
    }
}
public sealed class SearchCompaniesHandler(IOrganizationDbContext db, IAccessService access)
    : IRequestHandler<SearchCompaniesRequest, Page<CompanyDto>>
{
    public async Task<Page<CompanyDto>> Handle(SearchCompaniesRequest request, CancellationToken cancellationToken)
    {
        var companyId = IdValue.Parse(request.CompanyId);
        await access.DemandCompanyAsync(companyId, "DBRT01.VIEW", cancellationToken);
        var query = db.Companies.AsNoTracking().Where(x => x.Id == companyId);
        if (request.IsActive is not null) query = query.Where(x => x.IsActive == request.IsActive);
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
        var rows = await sorted.ThenBy(x => x.Id).Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize).ToListAsync(cancellationToken);
        return new(rows.Select(CompanyDto.Of).ToArray(), count, request.PageNumber, request.PageSize);
    }
}
