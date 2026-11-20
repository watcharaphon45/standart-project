using clean.Application.Common.Models;
using clean.Application.Features.DB.DBRT03;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace clean.Web.Controllers.DB;

[ApiController, Authorize, Route("api/db/dbrt03")]
public sealed class DBRT03Controller(ISender sender) : ControllerBase
{
    [HttpPost("search"), Authorize(Policy = "DBRT03.VIEW")]
    public Task<Page<DepartmentDto>> Search(SearchDepartmentsRequest request, CancellationToken cancellationToken) =>
        sender.Send(request, cancellationToken);

    [HttpPost("options"), Authorize]
    public Task<DepartmentOptionsDto> Options(GetDepartmentOptionsRequest request, CancellationToken cancellationToken) =>
        sender.Send(request, cancellationToken);

    [HttpPost("detail"), Authorize(Policy = "DBRT03.VIEW")]
    public Task<DepartmentDto> Detail(GetDepartmentDetailRequest request, CancellationToken cancellationToken) =>
        sender.Send(request, cancellationToken);

    [HttpPost("create"), Authorize(Policy = "DBRT03.CREATE")]
    public Task<DepartmentDto> Create(CreateDepartmentRequest request, CancellationToken cancellationToken) =>
        sender.Send(request, cancellationToken);

    [HttpPut("update"), Authorize(Policy = "DBRT03.UPDATE")]
    public Task<DepartmentDto> Update(UpdateDepartmentRequest request, CancellationToken cancellationToken) =>
        sender.Send(request, cancellationToken);

    [HttpPost("activate"), Authorize(Policy = "DBRT03.ACTIVATE")]
    public Task<DepartmentDto> Activate(StatusRequest request, CancellationToken cancellationToken) =>
        sender.Send(new ChangeDepartmentStatusRequest(request.Id, request.Version, true), cancellationToken);

    [HttpPost("deactivate"), Authorize(Policy = "DBRT03.DEACTIVATE")]
    public Task<DepartmentDto> Deactivate(StatusRequest request, CancellationToken cancellationToken) =>
        sender.Send(new ChangeDepartmentStatusRequest(request.Id, request.Version, false), cancellationToken);
}

