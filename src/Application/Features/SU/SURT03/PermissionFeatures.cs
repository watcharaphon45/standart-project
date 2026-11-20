using clean.Application.Common.Interfaces;
using clean.Application.Common.Models;
using clean.Application.Features.DB.Shared;
using clean.Domain.Security;
using FluentValidation;
using MediatR;

namespace clean.Application.Features.SU.SURT03;

public sealed record GetPermissionOptionsRequest(string? CompanyId = null) : IRequest<PermissionCatalogDto>;

public sealed class GetPermissionOptionsValidator : AbstractValidator<GetPermissionOptionsRequest>
{
    public GetPermissionOptionsValidator() => RuleFor(x => x.CompanyId).ValidOptionalId("รหัสบริษัทไม่ถูกต้อง");
}

public sealed class GetPermissionOptionsHandler(ISystemAdminService admin)
    : IRequestHandler<GetPermissionOptionsRequest, PermissionCatalogDto>
{
    public Task<PermissionCatalogDto> Handle(GetPermissionOptionsRequest request, CancellationToken cancellationToken) =>
        admin.GetPermissionOptionsAsync(request, cancellationToken);
}

public sealed record GetRolePermissionsDetailRequest(string RoleId) : IRequest<RolePermissionsDto>;

public sealed class GetRolePermissionsDetailValidator : AbstractValidator<GetRolePermissionsDetailRequest>
{
    public GetRolePermissionsDetailValidator() => RuleFor(x => x.RoleId).ValidId("รหัสบทบาทไม่ถูกต้อง");
}

public sealed class GetRolePermissionsDetailHandler(ISystemAdminService admin)
    : IRequestHandler<GetRolePermissionsDetailRequest, RolePermissionsDto>
{
    public Task<RolePermissionsDto> Handle(GetRolePermissionsDetailRequest request, CancellationToken cancellationToken) =>
        admin.GetRolePermissionsDetailAsync(request, cancellationToken);
}

public sealed class UpdateRolePermissionsRequest : IRequest<RolePermissionsDto>
{
    public string RoleId { get; set; } = "";
    public string[] Permissions { get; set; } = [];
    public string Version { get; set; } = "";
}

public sealed class UpdateRolePermissionsValidator : AbstractValidator<UpdateRolePermissionsRequest>
{
    public UpdateRolePermissionsValidator()
    {
        RuleFor(x => x.RoleId).ValidId("รหัสบทบาทไม่ถูกต้อง");
        RuleFor(x => x.Version).ValidId("กรุณาโหลดข้อมูลล่าสุดก่อนบันทึก");
        RuleFor(x => x.Permissions).NotNull();
        RuleForEach(x => x.Permissions)
            .Must(permission => ProgramRegistry.AllPermissions.Contains(permission))
            .WithMessage("พบสิทธิ์ที่ไม่อยู่ในทะเบียนระบบ");
    }
}

public sealed class UpdateRolePermissionsHandler(ISystemAdminService admin)
    : IRequestHandler<UpdateRolePermissionsRequest, RolePermissionsDto>
{
    public Task<RolePermissionsDto> Handle(UpdateRolePermissionsRequest request, CancellationToken cancellationToken) =>
        admin.UpdateRolePermissionsAsync(request, cancellationToken);
}

