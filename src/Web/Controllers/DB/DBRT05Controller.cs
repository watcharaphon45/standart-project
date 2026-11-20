using clean.Application.Common.Models;
using clean.Application.Features.DB.DBRT05;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace clean.Web.Controllers.DB;

[ApiController, Authorize, Route("api/db/dbrt05")]
public sealed class DBRT05Controller(ISender sender) : ControllerBase
{
    [HttpPost("search"), Authorize(Policy = "DBRT05.VIEW")]
    public Task<Page<PositionDto>> Search(SearchPositionsRequest request, CancellationToken cancellationToken) =>
        sender.Send(request, cancellationToken);

    [HttpPost("detail"), Authorize(Policy = "DBRT05.VIEW")]
    public Task<PositionDto> Detail(GetPositionDetailRequest request, CancellationToken cancellationToken) =>
        sender.Send(request, cancellationToken);

    [HttpPost("create"), Authorize(Policy = "DBRT05.CREATE")]
    public Task<PositionDto> Create(CreatePositionRequest request, CancellationToken cancellationToken) =>
        sender.Send(request, cancellationToken);

    [HttpPut("update"), Authorize(Policy = "DBRT05.UPDATE")]
    public Task<PositionDto> Update(UpdatePositionRequest request, CancellationToken cancellationToken) =>
        sender.Send(request, cancellationToken);

    [HttpPost("activate"), Authorize(Policy = "DBRT05.ACTIVATE")]
    public Task<PositionDto> Activate(StatusRequest request, CancellationToken cancellationToken) =>
        sender.Send(new ChangePositionStatusRequest(request.Id, request.Version, true), cancellationToken);

    [HttpPost("deactivate"), Authorize(Policy = "DBRT05.DEACTIVATE")]
    public Task<PositionDto> Deactivate(StatusRequest request, CancellationToken cancellationToken) =>
        sender.Send(new ChangePositionStatusRequest(request.Id, request.Version, false), cancellationToken);
}

