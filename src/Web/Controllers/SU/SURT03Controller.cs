using clean.Application.Common.Models;
using clean.Application.Features.SU.SURT03;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace clean.Web.Controllers.SU;

[ApiController, Authorize, Route("api/su/surt03")]
public sealed class SURT03Controller(ISender sender) : ControllerBase
{
    [HttpPost("options"), Authorize(Policy = "SURT03.VIEW")]
    public Task<PermissionCatalogDto> Options(GetPermissionOptionsRequest request, CancellationToken cancellationToken) =>
        sender.Send(request, cancellationToken);

    [HttpPost("detail"), Authorize(Policy = "SURT03.VIEW")]
    public Task<RolePermissionsDto> Detail(GetRolePermissionsDetailRequest request, CancellationToken cancellationToken) =>
        sender.Send(request, cancellationToken);

    [HttpPut("update"), Authorize(Policy = "SURT03.UPDATE")]
    public Task<RolePermissionsDto> Update(UpdateRolePermissionsRequest request, CancellationToken cancellationToken) =>
        sender.Send(request, cancellationToken);
}

