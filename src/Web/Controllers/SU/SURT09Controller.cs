using clean.Application.Common.Models;
using clean.Application.Features.SU.SURT09;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace clean.Web.Controllers.SU;

[ApiController, Authorize, Route("api/su/surt09")]
public sealed class SURT09Controller(ISender sender) : ControllerBase
{
    [HttpPost("search"), Authorize(Policy = "SURT09.VIEW")]
    public Task<Page<AuditEventItemDto>> Search(SearchAuditLogsRequest request, CancellationToken cancellationToken) =>
        sender.Send(request, cancellationToken);

    [HttpPost("detail"), Authorize(Policy = "SURT09.VIEW")]
    public Task<AuditEventDetailDto> Detail(GetAuditLogDetailRequest request, CancellationToken cancellationToken) =>
        sender.Send(request, cancellationToken);

    [HttpPost("export"), Authorize(Policy = "SURT09.EXPORT")]
    public Task<AuditExportDto> Export(ExportAuditLogsRequest request, CancellationToken cancellationToken) =>
        sender.Send(request, cancellationToken);
}

