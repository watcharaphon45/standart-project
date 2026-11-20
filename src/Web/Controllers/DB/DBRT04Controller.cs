using clean.Application.Common.Models;
using clean.Application.Features.DB.DBRT04;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace clean.Web.Controllers.DB;

[ApiController, Authorize, Route("api/db/dbrt04")]
public sealed class DBRT04Controller(ISender sender) : ControllerBase
{
    [HttpPost("search"), Authorize(Policy = "DBRT04.VIEW")]
    public Task<Page<DivisionDto>> Search(SearchDivisionsRequest request, CancellationToken cancellationToken) =>
        sender.Send(request, cancellationToken);

    [HttpPost("options"), Authorize]
    public Task<IReadOnlyList<LookupOptionDto>> Options(GetDivisionOptionsRequest request, CancellationToken cancellationToken) =>
        sender.Send(request, cancellationToken);

    [HttpPost("detail"), Authorize(Policy = "DBRT04.VIEW")]
    public Task<DivisionDto> Detail(GetDivisionDetailRequest request, CancellationToken cancellationToken) =>
        sender.Send(request, cancellationToken);

    [HttpPost("create"), Authorize(Policy = "DBRT04.CREATE")]
    public Task<DivisionDto> Create(CreateDivisionRequest request, CancellationToken cancellationToken) =>
        sender.Send(request, cancellationToken);

    [HttpPut("update"), Authorize(Policy = "DBRT04.UPDATE")]
    public Task<DivisionDto> Update(UpdateDivisionRequest request, CancellationToken cancellationToken) =>
        sender.Send(request, cancellationToken);

    [HttpPost("activate"), Authorize(Policy = "DBRT04.ACTIVATE")]
    public Task<DivisionDto> Activate(StatusRequest request, CancellationToken cancellationToken) =>
        sender.Send(new ChangeDivisionStatusRequest(request.Id, request.Version, true), cancellationToken);

    [HttpPost("deactivate"), Authorize(Policy = "DBRT04.DEACTIVATE")]
    public Task<DivisionDto> Deactivate(StatusRequest request, CancellationToken cancellationToken) =>
        sender.Send(new ChangeDivisionStatusRequest(request.Id, request.Version, false), cancellationToken);
}

