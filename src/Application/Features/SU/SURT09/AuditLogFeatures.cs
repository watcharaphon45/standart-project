using clean.Application.Common.Interfaces;
using clean.Application.Common.Models;
using clean.Application.Features.DB.Shared;
using FluentValidation;
using MediatR;

namespace clean.Application.Features.SU.SURT09;

public sealed record SearchAuditLogsRequest(
    string? CompanyId = null,
    string? ProgramCode = null,
    string? Action = null,
    string? Keyword = null,
    int PageNumber = 1,
    int PageSize = 25,
    string SortField = "occurredAtUtc",
    string SortDirection = "desc") : IRequest<Page<AuditEventItemDto>>;

public sealed class SearchAuditLogsValidator : AbstractValidator<SearchAuditLogsRequest>
{
    public SearchAuditLogsValidator()
    {
        RuleFor(x => x.CompanyId).ValidOptionalId("รหัสบริษัทไม่ถูกต้อง");
        RuleFor(x => x.ProgramCode).MaximumLength(20);
        RuleFor(x => x.Action).MaximumLength(50);
        OrganizationMasterValidation.ApplyPagingRules(
            this,
            x => x.Keyword,
            x => x.PageNumber,
            x => x.PageSize,
            x => x.SortField,
            x => x.SortDirection,
            "occurredAtUtc", "programCode", "action", "result");
    }
}

public sealed class SearchAuditLogsHandler(ISystemAdminService admin)
    : IRequestHandler<SearchAuditLogsRequest, Page<AuditEventItemDto>>
{
    public Task<Page<AuditEventItemDto>> Handle(SearchAuditLogsRequest request, CancellationToken cancellationToken) =>
        admin.SearchAuditLogsAsync(request, cancellationToken);
}

public sealed record GetAuditLogDetailRequest(string Id) : IRequest<AuditEventDetailDto>;

public sealed class GetAuditLogDetailValidator : AbstractValidator<GetAuditLogDetailRequest>
{
    public GetAuditLogDetailValidator() => RuleFor(x => x.Id).ValidId();
}

public sealed class GetAuditLogDetailHandler(ISystemAdminService admin)
    : IRequestHandler<GetAuditLogDetailRequest, AuditEventDetailDto>
{
    public Task<AuditEventDetailDto> Handle(GetAuditLogDetailRequest request, CancellationToken cancellationToken) =>
        admin.GetAuditLogDetailAsync(request, cancellationToken);
}

public sealed record ExportAuditLogsRequest(
    string? CompanyId = null,
    string? ProgramCode = null,
    string? Action = null,
    string? Keyword = null,
    string SortField = "occurredAtUtc",
    string SortDirection = "desc") : IRequest<AuditExportDto>;

public sealed class ExportAuditLogsValidator : AbstractValidator<ExportAuditLogsRequest>
{
    public ExportAuditLogsValidator()
    {
        RuleFor(x => x.CompanyId).ValidOptionalId("รหัสบริษัทไม่ถูกต้อง");
        RuleFor(x => x.ProgramCode).MaximumLength(20);
        RuleFor(x => x.Action).MaximumLength(50);
        RuleFor(x => x.Keyword).MaximumLength(200);
        RuleFor(x => x.SortField).Must(x => x is "occurredAtUtc" or "programCode" or "action" or "result");
        RuleFor(x => x.SortDirection).Must(x => x is "asc" or "desc");
    }
}

public sealed class ExportAuditLogsHandler(ISystemAdminService admin)
    : IRequestHandler<ExportAuditLogsRequest, AuditExportDto>
{
    public Task<AuditExportDto> Handle(ExportAuditLogsRequest request, CancellationToken cancellationToken) =>
        admin.ExportAuditLogsAsync(request, cancellationToken);
}

