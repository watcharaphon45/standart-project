using clean.Application.Common.Models;
using clean.Application.Features.SU.SURT08;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace clean.Web.Controllers.SU;

[ApiController, Authorize, Route("api/su/surt08")]
public sealed class SURT08Controller(ISender sender) : ControllerBase
{
    [HttpPost("search"), Authorize(Policy = "SURT08.VIEW")]
    public Task<Page<SystemSettingDto>> Search(SearchSystemSettingsRequest request, CancellationToken cancellationToken) =>
        sender.Send(request, cancellationToken);

    [HttpPost("detail"), Authorize(Policy = "SURT08.VIEW")]
    public Task<SystemSettingDto> Detail(GetSystemSettingDetailRequest request, CancellationToken cancellationToken) =>
        sender.Send(request, cancellationToken);

    [HttpPost("create"), Authorize(Policy = "SURT08.CREATE")]
    public Task<SystemSettingDto> Create(CreateSystemSettingRequest request, CancellationToken cancellationToken) =>
        sender.Send(request, cancellationToken);

    [HttpPut("update"), Authorize(Policy = "SURT08.UPDATE")]
    public Task<SystemSettingDto> Update(UpdateSystemSettingRequest request, CancellationToken cancellationToken) =>
        sender.Send(request, cancellationToken);
}

