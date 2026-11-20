using clean.Application.Common.Behaviors;
using clean.Application.Common.Interfaces;
using clean.Application.Common.Models;
using clean.Application.Features.DB.Shared;
using FluentValidation;
using MediatR;

namespace clean.Application.Features.SU.SURT05;

public sealed record SearchMenuItemsRequest(
    string? Keyword = null,
    string? ModuleCode = null,
    bool? IsActive = null,
    int PageNumber = 1,
    int PageSize = 25,
    string SortField = "sortOrder",
    string SortDirection = "asc") : IRequest<Page<MenuItemDto>>;

public sealed class SearchMenuItemsValidator : AbstractValidator<SearchMenuItemsRequest>
{
    public SearchMenuItemsValidator()
    {
        RuleFor(x => x.ModuleCode)
            .Must(x => string.IsNullOrEmpty(x) || x is "DB" or "SU")
            .WithMessage("รหัสโมดูลไม่ถูกต้อง");
        OrganizationMasterValidation.ApplyPagingRules(
            this,
            x => x.Keyword,
            x => x.PageNumber,
            x => x.PageSize,
            x => x.SortField,
            x => x.SortDirection,
            "sortOrder", "programCode", "displayName", "isActive");
    }
}

public sealed class SearchMenuItemsHandler(ISystemAdminService admin)
    : IRequestHandler<SearchMenuItemsRequest, Page<MenuItemDto>>
{
    public Task<Page<MenuItemDto>> Handle(SearchMenuItemsRequest request, CancellationToken cancellationToken) =>
        admin.SearchMenuItemsAsync(request, cancellationToken);
}

public sealed record GetMenuItemDetailRequest(string Id) : IRequest<MenuItemDto>;

public sealed class GetMenuItemDetailValidator : AbstractValidator<GetMenuItemDetailRequest>
{
    public GetMenuItemDetailValidator() => RuleFor(x => x.Id).ValidId();
}

public sealed class GetMenuItemDetailHandler(ISystemAdminService admin)
    : IRequestHandler<GetMenuItemDetailRequest, MenuItemDto>
{
    public Task<MenuItemDto> Handle(GetMenuItemDetailRequest request, CancellationToken cancellationToken) =>
        admin.GetMenuItemDetailAsync(request, cancellationToken);
}

public sealed class UpdateMenuItemRequest : IRequest<MenuItemDto>, INormalizableRequest
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public int SortOrder { get; set; }
    public string Version { get; set; } = "";

    public void Normalize() => DisplayName = (DisplayName ?? "").Trim();
}

public sealed class UpdateMenuItemValidator : AbstractValidator<UpdateMenuItemRequest>
{
    public UpdateMenuItemValidator()
    {
        RuleFor(x => x.Id).ValidId();
        RuleFor(x => x.Version).ValidId("กรุณาโหลดข้อมูลล่าสุดก่อนบันทึก");
        RuleFor(x => x.DisplayName).ValidName();
        RuleFor(x => x.SortOrder).InclusiveBetween(1, 9999).WithMessage("ลำดับการแสดงผลต้องอยู่ระหว่าง 1–9999");
    }
}

public sealed class UpdateMenuItemHandler(ISystemAdminService admin)
    : IRequestHandler<UpdateMenuItemRequest, MenuItemDto>
{
    public Task<MenuItemDto> Handle(UpdateMenuItemRequest request, CancellationToken cancellationToken) =>
        admin.UpdateMenuItemAsync(request, cancellationToken);
}

public sealed record ChangeMenuItemStatusRequest(string Id, string Version, bool IsActive) : IRequest<MenuItemDto>;

public sealed class ChangeMenuItemStatusValidator : AbstractValidator<ChangeMenuItemStatusRequest>
{
    public ChangeMenuItemStatusValidator()
    {
        RuleFor(x => x.Id).ValidId();
        RuleFor(x => x.Version).ValidId("กรุณาโหลดข้อมูลล่าสุดก่อนบันทึก");
    }
}

public sealed class ChangeMenuItemStatusHandler(ISystemAdminService admin)
    : IRequestHandler<ChangeMenuItemStatusRequest, MenuItemDto>
{
    public Task<MenuItemDto> Handle(ChangeMenuItemStatusRequest request, CancellationToken cancellationToken) =>
        admin.ChangeMenuItemStatusAsync(request, cancellationToken);
}

