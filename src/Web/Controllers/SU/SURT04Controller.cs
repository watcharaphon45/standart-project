using clean.Application.Common.Models;
using clean.Application.Features.SU.SURT04;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace clean.Web.Controllers.SU;

[ApiController, Authorize, Route("api/su/surt04")]
public sealed class SURT04Controller(ISender sender) : ControllerBase
{
    [HttpPost("search"), Authorize(Policy = "SURT04.VIEW")]
    public Task<Page<UserScopeListItemDto>> Search(SearchUserScopesRequest request, CancellationToken cancellationToken) =>
        sender.Send(request, cancellationToken);

    [HttpPost("options"), Authorize(Policy = "SURT04.VIEW")]
    public Task<UserScopeOptionsDto> Options(GetUserScopeOptionsRequest request, CancellationToken cancellationToken) =>
        sender.Send(request, cancellationToken);

    [HttpPost("detail"), Authorize(Policy = "SURT04.VIEW")]
    public Task<UserScopeDetailDto> Detail(GetUserScopeDetailRequest request, CancellationToken cancellationToken) =>
        sender.Send(request, cancellationToken);

    [HttpPut("update"), Authorize(Policy = "SURT04.UPDATE")]
    public Task<UserScopeDetailDto> Update(UpdateUserScopeRequest request, CancellationToken cancellationToken) =>
        sender.Send(request, cancellationToken);
}

