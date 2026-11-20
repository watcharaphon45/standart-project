using clean.Application.Common.Behaviors;
using clean.Application.Common.Interfaces;
using clean.Application.Common.Models;
using clean.Application.Features.DB.Shared;
using clean.Domain.Entities.Organization;
using FluentValidation;
using MediatR;

namespace clean.Application.Features.SU.SURT08;

public sealed record SearchSystemSettingsRequest(
    string? CompanyId = null,
    string? Keyword = null,
    int PageNumber = 1,
    int PageSize = 25,
    string SortField = "key",
    string SortDirection = "asc") : IRequest<Page<SystemSettingDto>>;

public sealed class SearchSystemSettingsValidator : AbstractValidator<SearchSystemSettingsRequest>
{
    public SearchSystemSettingsValidator()
    {
        RuleFor(x => x.CompanyId).ValidOptionalId("รหัสบริษัทไม่ถูกต้อง");
        OrganizationMasterValidation.ApplyPagingRules(
            this,
            x => x.Keyword,
            x => x.PageNumber,
            x => x.PageSize,
            x => x.SortField,
            x => x.SortDirection,
            "key", "displayName", "dataType");
    }
}

public sealed class SearchSystemSettingsHandler(ISystemAdminService admin)
    : IRequestHandler<SearchSystemSettingsRequest, Page<SystemSettingDto>>
{
    public Task<Page<SystemSettingDto>> Handle(SearchSystemSettingsRequest request, CancellationToken cancellationToken) =>
        admin.SearchSystemSettingsAsync(request, cancellationToken);
}

public sealed record GetSystemSettingDetailRequest(string Id) : IRequest<SystemSettingDto>;

public sealed class GetSystemSettingDetailValidator : AbstractValidator<GetSystemSettingDetailRequest>
{
    public GetSystemSettingDetailValidator() => RuleFor(x => x.Id).ValidId();
}

public sealed class GetSystemSettingDetailHandler(ISystemAdminService admin)
    : IRequestHandler<GetSystemSettingDetailRequest, SystemSettingDto>
{
    public Task<SystemSettingDto> Handle(GetSystemSettingDetailRequest request, CancellationToken cancellationToken) =>
        admin.GetSystemSettingDetailAsync(request, cancellationToken);
}

public sealed class CreateSystemSettingRequest : IRequest<SystemSettingDto>, INormalizableRequest
{
    public string? CompanyId { get; set; }
    public string Key { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string DataType { get; set; } = "STRING";
    public string Value { get; set; } = "";
    public bool IsSensitive { get; set; }

    public void Normalize()
    {
        CompanyId = string.IsNullOrWhiteSpace(CompanyId) ? null : CompanyId.Trim();
        Key = Company.NormalizeCode(Key);
        DisplayName = (DisplayName ?? "").Trim();
        DataType = Company.NormalizeCode(DataType);
        Value = (Value ?? "").Trim();
    }
}

public sealed class CreateSystemSettingValidator : AbstractValidator<CreateSystemSettingRequest>
{
    public CreateSystemSettingValidator()
    {
        RuleFor(x => x.CompanyId).ValidOptionalId("รหัสบริษัทไม่ถูกต้อง");
        RuleFor(x => x.Key)
            .NotEmpty().WithMessage("กรุณาระบุคีย์ตั้งค่า")
            .Matches(@"\A[A-Z0-9._-]{1,50}\z")
            .WithMessage("คีย์ใช้ได้เฉพาะ A–Z, 0–9, จุด, - และ _ สูงสุด 50 ตัวอักษร");
        RuleFor(x => x.DisplayName).ValidName();
        RuleFor(x => x.DataType)
            .Must(x => x is "STRING" or "INTEGER" or "BOOLEAN")
            .WithMessage("ชนิดข้อมูลต้องเป็น STRING, INTEGER หรือ BOOLEAN");
        RuleFor(x => x.Value).MaximumLength(500).WithMessage("ค่าตั้งค่าต้องไม่เกิน 500 ตัวอักษร");
    }
}

public sealed class CreateSystemSettingHandler(ISystemAdminService admin)
    : IRequestHandler<CreateSystemSettingRequest, SystemSettingDto>
{
    public Task<SystemSettingDto> Handle(CreateSystemSettingRequest request, CancellationToken cancellationToken) =>
        admin.CreateSystemSettingAsync(request, cancellationToken);
}

public sealed class UpdateSystemSettingRequest : IRequest<SystemSettingDto>, INormalizableRequest
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Value { get; set; } = "";
    public string Version { get; set; } = "";

    public void Normalize()
    {
        DisplayName = (DisplayName ?? "").Trim();
        Value = (Value ?? "").Trim();
    }
}

public sealed class UpdateSystemSettingValidator : AbstractValidator<UpdateSystemSettingRequest>
{
    public UpdateSystemSettingValidator()
    {
        RuleFor(x => x.Id).ValidId();
        RuleFor(x => x.Version).ValidId("กรุณาโหลดข้อมูลล่าสุดก่อนบันทึก");
        RuleFor(x => x.DisplayName).ValidName();
        RuleFor(x => x.Value).MaximumLength(500).WithMessage("ค่าตั้งค่าต้องไม่เกิน 500 ตัวอักษร");
    }
}

public sealed class UpdateSystemSettingHandler(ISystemAdminService admin)
    : IRequestHandler<UpdateSystemSettingRequest, SystemSettingDto>
{
    public Task<SystemSettingDto> Handle(UpdateSystemSettingRequest request, CancellationToken cancellationToken) =>
        admin.UpdateSystemSettingAsync(request, cancellationToken);
}

