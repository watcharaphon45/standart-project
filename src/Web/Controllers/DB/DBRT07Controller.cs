using clean.Application.Common.Models;
using clean.Application.Features.DB.DBRT07;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace clean.Web.Controllers.DB;

[ApiController, Authorize, Route("api/db/dbrt07")]
public sealed class DBRT07Controller(ISender sender) : ControllerBase
{
    [HttpPost("search"), Authorize(Policy = "DBRT07.VIEW")]
    public Task<Page<BusinessUnitDto>> Search(SearchBusinessUnitsRequest request, CancellationToken cancellationToken) =>
        sender.Send(request, cancellationToken);

    [HttpPost("detail"), Authorize(Policy = "DBRT07.VIEW")]
    public Task<BusinessUnitDto> Detail(GetBusinessUnitDetailRequest request, CancellationToken cancellationToken) =>
        sender.Send(request, cancellationToken);

    [HttpPost("create"), Authorize(Policy = "DBRT07.CREATE")]
    public Task<BusinessUnitDto> Create(CreateBusinessUnitRequest request, CancellationToken cancellationToken) =>
        sender.Send(request, cancellationToken);

    [HttpPut("update"), Authorize(Policy = "DBRT07.UPDATE")]
    public Task<BusinessUnitDto> Update(UpdateBusinessUnitRequest request, CancellationToken cancellationToken) =>
        sender.Send(request, cancellationToken);

    [HttpPost("activate"), Authorize(Policy = "DBRT07.ACTIVATE")]
    public Task<BusinessUnitDto> Activate(StatusRequest request, CancellationToken cancellationToken) =>
        sender.Send(new ChangeBusinessUnitStatusRequest(request.Id, request.Version, true), cancellationToken);

    [HttpPost("deactivate"), Authorize(Policy = "DBRT07.DEACTIVATE")]
    public Task<BusinessUnitDto> Deactivate(StatusRequest request, CancellationToken cancellationToken) =>
        sender.Send(new ChangeBusinessUnitStatusRequest(request.Id, request.Version, false), cancellationToken);
}

