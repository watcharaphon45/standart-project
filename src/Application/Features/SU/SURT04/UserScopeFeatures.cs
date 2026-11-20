using clean.Application.Common.Interfaces;
using clean.Application.Common.Models;
using clean.Application.Features.DB.Shared;
using FluentValidation;
using MediatR;

namespace clean.Application.Features.SU.SURT04;

public sealed record SearchUserScopesRequest(
    string CompanyId,
    string? Keyword = null,
    string? ScopeType = null,
    bool? IsActive = null,
    int PageNumber = 1,
    int PageSize = 25,
    string SortField = "userName",
    string SortDirection = "asc") : IRequest<Page<UserScopeListItemDto>>;

public sealed class SearchUserScopesValidator : AbstractValidator<SearchUserScopesRequest>
{
    public SearchUserScopesValidator()
    {
        RuleFor(x => x.CompanyId).ValidId("กรุณาเลือกบริษัท");
        RuleFor(x => x.ScopeType)
            .Must(x => string.IsNullOrEmpty(x) || x is "Company" or "Branch" or "Department")
            .WithMessage("ประเภทขอบเขตไม่ถูกต้อง");
        OrganizationMasterValidation.ApplyPagingRules(
            this,
            x => x.Keyword,
            x => x.PageNumber,
            x => x.PageSize,
            x => x.SortField,
            x => x.SortDirection,
            "userName", "email", "roleCode", "scopeType", "isActive");
    }
}

public sealed class SearchUserScopesHandler(ISystemAdminService admin)
    : IRequestHandler<SearchUserScopesRequest, Page<UserScopeListItemDto>>
{
    public Task<Page<UserScopeListItemDto>> Handle(SearchUserScopesRequest request, CancellationToken cancellationToken) =>
        admin.SearchUserScopesAsync(request, cancellationToken);
}

public sealed record GetUserScopeOptionsRequest(string CompanyId) : IRequest<UserScopeOptionsDto>;

public sealed class GetUserScopeOptionsValidator : AbstractValidator<GetUserScopeOptionsRequest>
{
    public GetUserScopeOptionsValidator() => RuleFor(x => x.CompanyId).ValidId("กรุณาเลือกบริษัท");
}

public sealed class GetUserScopeOptionsHandler(ISystemAdminService admin)
    : IRequestHandler<GetUserScopeOptionsRequest, UserScopeOptionsDto>
{
    public Task<UserScopeOptionsDto> Handle(GetUserScopeOptionsRequest request, CancellationToken cancellationToken) =>
        admin.GetUserScopeOptionsAsync(request, cancellationToken);
}

public sealed record GetUserScopeDetailRequest(string CompanyId, string UserId) : IRequest<UserScopeDetailDto>;

public sealed class GetUserScopeDetailValidator : AbstractValidator<GetUserScopeDetailRequest>
{
    public GetUserScopeDetailValidator()
    {
        RuleFor(x => x.CompanyId).ValidId("กรุณาเลือกบริษัท");
        RuleFor(x => x.UserId).ValidId("รหัสผู้ใช้งานไม่ถูกต้อง");
    }
}

public sealed class GetUserScopeDetailHandler(ISystemAdminService admin)
    : IRequestHandler<GetUserScopeDetailRequest, UserScopeDetailDto>
{
    public Task<UserScopeDetailDto> Handle(GetUserScopeDetailRequest request, CancellationToken cancellationToken) =>
        admin.GetUserScopeDetailAsync(request, cancellationToken);
}

public sealed record UpdateUserScopeRequest(
    string CompanyId,
    string UserId,
    string RoleId,
    string ScopeType,
    string? ScopeId,
    bool IsActive,
    string? Version = null) : IRequest<UserScopeDetailDto>;

public sealed class UpdateUserScopeValidator : AbstractValidator<UpdateUserScopeRequest>
{
    public UpdateUserScopeValidator()
    {
        RuleFor(x => x.CompanyId).ValidId("กรุณาเลือกบริษัท");
        RuleFor(x => x.UserId).ValidId("รหัสผู้ใช้งานไม่ถูกต้อง");
        RuleFor(x => x.RoleId).ValidId("กรุณาเลือกบทบาท");
        RuleFor(x => x.ScopeType)
            .Must(x => x is "Company" or "Branch" or "Department")
            .WithMessage("ประเภทขอบเขตไม่ถูกต้อง");
        When(x => x.ScopeType is "Branch" or "Department", () =>
            RuleFor(x => x.ScopeId).Must(IdValue.IsValid).WithMessage("กรุณาเลือกสาขาหรือแผนก"));
        When(x => x.ScopeType == "Company", () =>
            RuleFor(x => x.ScopeId).Empty().WithMessage("ขอบเขตระดับบริษัทต้องไม่ระบุสาขาหรือแผนก"));
        RuleFor(x => x.Version).ValidOptionalId("กรุณาโหลดข้อมูลล่าสุดก่อนบันทึก");
    }
}

public sealed class UpdateUserScopeHandler(ISystemAdminService admin)
    : IRequestHandler<UpdateUserScopeRequest, UserScopeDetailDto>
{
    public Task<UserScopeDetailDto> Handle(UpdateUserScopeRequest request, CancellationToken cancellationToken) =>
        admin.UpdateUserScopeAsync(request, cancellationToken);
}

