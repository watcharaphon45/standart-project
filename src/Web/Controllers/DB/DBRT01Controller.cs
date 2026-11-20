using clean.Application.Common.Models;
using clean.Application.Features.DB.DBRT01.ChangeCompanyStatus;
using clean.Application.Features.DB.DBRT01.CreateCompany;
using clean.Application.Features.DB.DBRT01.GetCompanyDetail;
using clean.Application.Features.DB.DBRT01.SearchCompanies;
using clean.Application.Features.DB.DBRT01.UpdateCompany;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace clean.Web.Controllers.DB;

[ApiController, Authorize, Route("api/db/dbrt01")]
public sealed class DBRT01Controller(ISender sender) : ControllerBase
{
    [HttpPost("search"), Authorize(Policy = "DBRT01.VIEW")]
    public Task<Page<CompanyDto>> Search(SearchCompaniesRequest request, CancellationToken cancellationToken) => sender.Send(request, cancellationToken);

    [HttpPost("detail"), Authorize(Policy = "DBRT01.VIEW")]
    public Task<CompanyDto> Detail(GetCompanyDetailRequest request, CancellationToken cancellationToken) => sender.Send(request, cancellationToken);

    [HttpPost("create"), Authorize(Policy = "DBRT01.CREATE")]
    public Task<CompanyDto> Create(CreateCompanyRequest request, CancellationToken cancellationToken) => sender.Send(request, cancellationToken);

    [HttpPut("update"), Authorize(Policy = "DBRT01.UPDATE")]
    public Task<CompanyDto> Update(UpdateCompanyRequest request, CancellationToken cancellationToken) => sender.Send(request, cancellationToken);

    [HttpPost("activate"), Authorize(Policy = "DBRT01.ACTIVATE")]
    public Task<CompanyDto> Activate(StatusRequest request, CancellationToken cancellationToken) =>
        sender.Send(new ChangeCompanyStatusRequest(request.Id, request.Version, true), cancellationToken);

    [HttpPost("deactivate"), Authorize(Policy = "DBRT01.DEACTIVATE")]
    public Task<CompanyDto> Deactivate(StatusRequest request, CancellationToken cancellationToken) =>
        sender.Send(new ChangeCompanyStatusRequest(request.Id, request.Version, false), cancellationToken);
}
public sealed record StatusRequest(string Id, string Version);
