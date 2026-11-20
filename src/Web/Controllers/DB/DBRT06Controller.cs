using clean.Application.Common.Models;
using clean.Application.Features.DB.DBRT06;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace clean.Web.Controllers.DB;

[ApiController, Authorize, Route("api/db/dbrt06")]
public sealed class DBRT06Controller(ISender sender) : ControllerBase
{
    [HttpPost("search"), Authorize(Policy = "DBRT06.VIEW")]
    public Task<Page<CostCenterDto>> Search(SearchCostCentersRequest request, CancellationToken cancellationToken) =>
        sender.Send(request, cancellationToken);

    [HttpPost("detail"), Authorize(Policy = "DBRT06.VIEW")]
    public Task<CostCenterDto> Detail(GetCostCenterDetailRequest request, CancellationToken cancellationToken) =>
        sender.Send(request, cancellationToken);

    [HttpPost("create"), Authorize(Policy = "DBRT06.CREATE")]
    public Task<CostCenterDto> Create(CreateCostCenterRequest request, CancellationToken cancellationToken) =>
        sender.Send(request, cancellationToken);

    [HttpPut("update"), Authorize(Policy = "DBRT06.UPDATE")]
    public Task<CostCenterDto> Update(UpdateCostCenterRequest request, CancellationToken cancellationToken) =>
        sender.Send(request, cancellationToken);

    [HttpPost("activate"), Authorize(Policy = "DBRT06.ACTIVATE")]
    public Task<CostCenterDto> Activate(StatusRequest request, CancellationToken cancellationToken) =>
        sender.Send(new ChangeCostCenterStatusRequest(request.Id, request.Version, true), cancellationToken);

    [HttpPost("deactivate"), Authorize(Policy = "DBRT06.DEACTIVATE")]
    public Task<CostCenterDto> Deactivate(StatusRequest request, CancellationToken cancellationToken) =>
        sender.Send(new ChangeCostCenterStatusRequest(request.Id, request.Version, false), cancellationToken);
}

