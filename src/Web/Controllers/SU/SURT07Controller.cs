using clean.Application.Common.Models;
using clean.Application.Features.SU.SURT07;
using clean.Web.Controllers.DB;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace clean.Web.Controllers.SU;

[ApiController, Authorize, Route("api/su/surt07")]
public sealed class SURT07Controller(ISender sender) : ControllerBase
{
    [HttpPost("search"), Authorize(Policy = "SURT07.VIEW")]
    public Task<Page<ApprovalWorkflowDto>> Search(SearchApprovalWorkflowsRequest request, CancellationToken cancellationToken) =>
        sender.Send(request, cancellationToken);

    [HttpPost("options"), Authorize(Policy = "SURT07.VIEW")]
    public Task<ApprovalWorkflowOptionsDto> Options(GetApprovalWorkflowOptionsRequest request, CancellationToken cancellationToken) =>
        sender.Send(request, cancellationToken);

    [HttpPost("detail"), Authorize(Policy = "SURT07.VIEW")]
    public Task<ApprovalWorkflowDetailDto> Detail(GetApprovalWorkflowDetailRequest request, CancellationToken cancellationToken) =>
        sender.Send(request, cancellationToken);

    [HttpPost("create"), Authorize(Policy = "SURT07.CREATE")]
    public Task<ApprovalWorkflowDetailDto> Create(CreateApprovalWorkflowRequest request, CancellationToken cancellationToken) =>
        sender.Send(request, cancellationToken);

    [HttpPut("update"), Authorize(Policy = "SURT07.UPDATE")]
    public Task<ApprovalWorkflowDetailDto> Update(UpdateApprovalWorkflowRequest request, CancellationToken cancellationToken) =>
        sender.Send(request, cancellationToken);

    [HttpPost("activate"), Authorize(Policy = "SURT07.ACTIVATE")]
    public Task<ApprovalWorkflowDto> Activate(StatusRequest request, CancellationToken cancellationToken) =>
        sender.Send(new ChangeApprovalWorkflowStatusRequest(request.Id, request.Version, true), cancellationToken);

    [HttpPost("deactivate"), Authorize(Policy = "SURT07.DEACTIVATE")]
    public Task<ApprovalWorkflowDto> Deactivate(StatusRequest request, CancellationToken cancellationToken) =>
        sender.Send(new ChangeApprovalWorkflowStatusRequest(request.Id, request.Version, false), cancellationToken);
}

