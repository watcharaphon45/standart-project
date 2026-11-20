using clean.Application.Common.Models;
using clean.Application.Features.DB.DBRT02;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace clean.Web.Controllers.DB;

[ApiController, Authorize, Route("api/db/dbrt02")]
public sealed class DBRT02Controller(ISender sender) : ControllerBase
{
    [HttpPost("search"), Authorize(Policy = "DBRT02.VIEW")]
    public Task<Page<BranchDto>> Search(SearchBranchesRequest request, CancellationToken cancellationToken) =>
        sender.Send(request, cancellationToken);

    [HttpPost("detail"), Authorize(Policy = "DBRT02.VIEW")]
    public Task<BranchDto> Detail(GetBranchDetailRequest request, CancellationToken cancellationToken) =>
        sender.Send(request, cancellationToken);

    [HttpPost("create"), Authorize(Policy = "DBRT02.CREATE")]
    public Task<BranchDto> Create(CreateBranchRequest request, CancellationToken cancellationToken) =>
        sender.Send(request, cancellationToken);

    [HttpPut("update"), Authorize(Policy = "DBRT02.UPDATE")]
    public Task<BranchDto> Update(UpdateBranchRequest request, CancellationToken cancellationToken) =>
        sender.Send(request, cancellationToken);

    [HttpPost("activate"), Authorize(Policy = "DBRT02.ACTIVATE")]
    public Task<BranchDto> Activate(StatusRequest request, CancellationToken cancellationToken) =>
        sender.Send(new ChangeBranchStatusRequest(request.Id, request.Version, true), cancellationToken);

    [HttpPost("deactivate"), Authorize(Policy = "DBRT02.DEACTIVATE")]
    public Task<BranchDto> Deactivate(StatusRequest request, CancellationToken cancellationToken) =>
        sender.Send(new ChangeBranchStatusRequest(request.Id, request.Version, false), cancellationToken);
}

