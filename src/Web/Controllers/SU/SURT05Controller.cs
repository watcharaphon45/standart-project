using clean.Application.Common.Models;
using clean.Application.Features.SU.SURT05;
using clean.Web.Controllers.DB;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace clean.Web.Controllers.SU;

[ApiController, Authorize, Route("api/su/surt05")]
public sealed class SURT05Controller(ISender sender) : ControllerBase
{
    [HttpPost("search"), Authorize(Policy = "SURT05.VIEW")]
    public Task<Page<MenuItemDto>> Search(SearchMenuItemsRequest request, CancellationToken cancellationToken) =>
        sender.Send(request, cancellationToken);

    [HttpPost("detail"), Authorize(Policy = "SURT05.VIEW")]
    public Task<MenuItemDto> Detail(GetMenuItemDetailRequest request, CancellationToken cancellationToken) =>
        sender.Send(request, cancellationToken);

    [HttpPut("update"), Authorize(Policy = "SURT05.UPDATE")]
    public Task<MenuItemDto> Update(UpdateMenuItemRequest request, CancellationToken cancellationToken) =>
        sender.Send(request, cancellationToken);

    [HttpPost("activate"), Authorize(Policy = "SURT05.ACTIVATE")]
    public Task<MenuItemDto> Activate(StatusRequest request, CancellationToken cancellationToken) =>
        sender.Send(new ChangeMenuItemStatusRequest(request.Id, request.Version, true), cancellationToken);

    [HttpPost("deactivate"), Authorize(Policy = "SURT05.DEACTIVATE")]
    public Task<MenuItemDto> Deactivate(StatusRequest request, CancellationToken cancellationToken) =>
        sender.Send(new ChangeMenuItemStatusRequest(request.Id, request.Version, false), cancellationToken);
}

