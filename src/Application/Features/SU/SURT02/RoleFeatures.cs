using clean.Application.Common.Behaviors;
using clean.Application.Common.Interfaces;
using clean.Application.Common.Models;
using clean.Application.Features.DB.Shared;
using clean.Domain.Entities.Organization;
using FluentValidation;
using MediatR;

namespace clean.Application.Features.SU.SURT02;

public sealed record SearchRolesRequest(
    string? CompanyId = null,
    string? Keyword = null,
    bool? IsActive = null,
    int PageNumber = 1,
    int PageSize = 25,
    string SortField = "code",
    string SortDirection = "asc") : IRequest<Page<RoleDto>>;

public sealed class SearchRolesValidator : AbstractValidator<SearchRolesRequest>
{
    public SearchRolesValidator()
    {
        RuleFor(x => x.CompanyId).ValidOptionalId("กรุณาเลือกบริษัท");
        OrganizationMasterValidation.ApplyPagingRules(
            this,
            x => x.Keyword,
            x => x.PageNumber,
            x => x.PageSize,
            x => x.SortField,
            x => x.SortDirection,
            "code", "name", "isActive");
    }
}

public sealed class SearchRolesHandler(ISystemAdminService admin)
    : IRequestHandler<SearchRolesRequest, Page<RoleDto>>
{
    public Task<Page<RoleDto>> Handle(SearchRolesRequest request, CancellationToken cancellationToken) =>
        admin.SearchRolesAsync(request, cancellationToken);
}

public sealed record GetRoleDetailRequest(string Id) : IRequest<RoleDto>;

public sealed class GetRoleDetailValidator : AbstractValidator<GetRoleDetailRequest>
{
    public GetRoleDetailValidator() => RuleFor(x => x.Id).ValidId();
}

public sealed class GetRoleDetailHandler(ISystemAdminService admin)
    : IRequestHandler<GetRoleDetailRequest, RoleDto>
{
    public Task<RoleDto> Handle(GetRoleDetailRequest request, CancellationToken cancellationToken) =>
        admin.GetRoleDetailAsync(request, cancellationToken);
}

public sealed class CreateRoleRequest : IRequest<RoleDto>, INormalizableRequest
{
    public string? CompanyId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";

    public void Normalize()
    {
        CompanyId = string.IsNullOrWhiteSpace(CompanyId) ? null : CompanyId.Trim();
        Code = Company.NormalizeCode(Code);
        Name = (Name ?? "").Trim();
    }
}

public sealed class CreateRoleValidator : AbstractValidator<CreateRoleRequest>
{
    public CreateRoleValidator()
    {
        RuleFor(x => x.CompanyId).ValidOptionalId("รหัสบริษัทไม่ถูกต้อง");
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).ValidName();
    }
}

public sealed class CreateRoleHandler(ISystemAdminService admin)
    : IRequestHandler<CreateRoleRequest, RoleDto>
{
    public Task<RoleDto> Handle(CreateRoleRequest request, CancellationToken cancellationToken) =>
        admin.CreateRoleAsync(request, cancellationToken);
}

public sealed class UpdateRoleRequest : IRequest<RoleDto>, INormalizableRequest
{
    public string Id { get; set; } = "";
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";

    public void Normalize()
    {
        Code = Company.NormalizeCode(Code);
        Name = (Name ?? "").Trim();
    }
}

public sealed class UpdateRoleValidator : AbstractValidator<UpdateRoleRequest>
{
    public UpdateRoleValidator()
    {
        RuleFor(x => x.Id).ValidId();
        RuleFor(x => x.Version).ValidId("กรุณาโหลดข้อมูลล่าสุดก่อนบันทึก");
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).ValidName();
    }
}

public sealed class UpdateRoleHandler(ISystemAdminService admin)
    : IRequestHandler<UpdateRoleRequest, RoleDto>
{
    public Task<RoleDto> Handle(UpdateRoleRequest request, CancellationToken cancellationToken) =>
        admin.UpdateRoleAsync(request, cancellationToken);
}

public sealed record ChangeRoleStatusRequest(string Id, string Version, bool IsActive) : IRequest<RoleDto>;

public sealed class ChangeRoleStatusValidator : AbstractValidator<ChangeRoleStatusRequest>
{
    public ChangeRoleStatusValidator()
    {
        RuleFor(x => x.Id).ValidId();
        RuleFor(x => x.Version).ValidId("กรุณาโหลดข้อมูลล่าสุดก่อนบันทึก");
    }
}

public sealed class ChangeRoleStatusHandler(ISystemAdminService admin)
    : IRequestHandler<ChangeRoleStatusRequest, RoleDto>
{
    public Task<RoleDto> Handle(ChangeRoleStatusRequest request, CancellationToken cancellationToken) =>
        admin.ChangeRoleStatusAsync(request, cancellationToken);
}

