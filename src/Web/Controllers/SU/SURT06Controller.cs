using clean.Application.Common.Models;
using clean.Application.Features.SU.SURT06;
using clean.Web.Controllers.DB;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace clean.Web.Controllers.SU;

[ApiController, Authorize, Route("api/su/surt06")]
public sealed class SURT06Controller(ISender sender) : ControllerBase
{
    [HttpPost("search"), Authorize(Policy = "SURT06.VIEW")]
    public Task<Page<DocumentSequenceDto>> Search(SearchDocumentSequencesRequest request, CancellationToken cancellationToken) =>
        sender.Send(request, cancellationToken);

    [HttpPost("detail"), Authorize(Policy = "SURT06.VIEW")]
    public Task<DocumentSequenceDto> Detail(GetDocumentSequenceDetailRequest request, CancellationToken cancellationToken) =>
        sender.Send(request, cancellationToken);

    [HttpPost("create"), Authorize(Policy = "SURT06.CREATE")]
    public Task<DocumentSequenceDto> Create(CreateDocumentSequenceRequest request, CancellationToken cancellationToken) =>
        sender.Send(request, cancellationToken);

    [HttpPut("update"), Authorize(Policy = "SURT06.UPDATE")]
    public Task<DocumentSequenceDto> Update(UpdateDocumentSequenceRequest request, CancellationToken cancellationToken) =>
        sender.Send(request, cancellationToken);

    [HttpPost("activate"), Authorize(Policy = "SURT06.ACTIVATE")]
    public Task<DocumentSequenceDto> Activate(StatusRequest request, CancellationToken cancellationToken) =>
        sender.Send(new ChangeDocumentSequenceStatusRequest(request.Id, request.Version, true), cancellationToken);

    [HttpPost("deactivate"), Authorize(Policy = "SURT06.DEACTIVATE")]
    public Task<DocumentSequenceDto> Deactivate(StatusRequest request, CancellationToken cancellationToken) =>
        sender.Send(new ChangeDocumentSequenceStatusRequest(request.Id, request.Version, false), cancellationToken);

    [HttpPost("next"), Authorize(Policy = "SURT06.UPDATE")]
    public Task<DocumentNumberAllocationDto> Next(AllocateDocumentNumberRequest request, CancellationToken cancellationToken) =>
        sender.Send(request, cancellationToken);
}

