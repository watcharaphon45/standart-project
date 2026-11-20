using clean.Application.Common.Behaviors;
using clean.Application.Common.Interfaces;
using clean.Application.Common.Models;
using clean.Application.Features.DB.Shared;
using FluentValidation;
using MediatR;

namespace clean.Application.Features.SU.SURT01;

internal static class UserValidation
{
    public static IRuleBuilderOptions<T, string> ValidUserName<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("กรุณาระบุชื่อผู้ใช้งาน")
            .Matches(@"\A[A-Za-z0-9._-]{1,100}\z")
            .WithMessage("ชื่อผู้ใช้งานใช้ได้เฉพาะ A–Z, a–z, 0–9, จุด, - และ _ สูงสุด 100 ตัวอักษร (ห้ามมี @ หรือช่องว่าง)");

    public static IRuleBuilderOptions<T, string> ValidEmail<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("กรุณาระบุอีเมล")
            .MaximumLength(256).WithMessage("อีเมลต้องไม่เกิน 256 ตัวอักษร")
            .EmailAddress().WithMessage("รูปแบบอีเมลไม่ถูกต้อง");

    public static IRuleBuilderOptions<T, string> ValidTemporaryPassword<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("กรุณาระบุรหัสผ่านชั่วคราว")
            .Length(15, 128).WithMessage("รหัสผ่านชั่วคราวต้องยาว 15–128 ตัวอักษร");
}

public sealed record SearchUsersRequest(
    string? Keyword = null,
    bool? IsActive = null,
    int PageNumber = 1,
    int PageSize = 25,
    string SortField = "userName",
    string SortDirection = "asc") : IRequest<Page<UserDto>>;

public sealed class SearchUsersValidator : AbstractValidator<SearchUsersRequest>
{
    public SearchUsersValidator() => OrganizationMasterValidation.ApplyPagingRules(
        this,
        x => x.Keyword,
        x => x.PageNumber,
        x => x.PageSize,
        x => x.SortField,
        x => x.SortDirection,
        "userName", "email", "isActive");
}

public sealed class SearchUsersHandler(ISystemAdminService admin)
    : IRequestHandler<SearchUsersRequest, Page<UserDto>>
{
    public Task<Page<UserDto>> Handle(SearchUsersRequest request, CancellationToken cancellationToken) =>
        admin.SearchUsersAsync(request, cancellationToken);
}

public sealed record GetUserDetailRequest(string Id) : IRequest<UserDto>;

public sealed class GetUserDetailValidator : AbstractValidator<GetUserDetailRequest>
{
    public GetUserDetailValidator() => RuleFor(x => x.Id).ValidId();
}

public sealed class GetUserDetailHandler(ISystemAdminService admin)
    : IRequestHandler<GetUserDetailRequest, UserDto>
{
    public Task<UserDto> Handle(GetUserDetailRequest request, CancellationToken cancellationToken) =>
        admin.GetUserDetailAsync(request, cancellationToken);
}

public sealed class CreateUserRequest : IRequest<UserDto>, INormalizableRequest
{
    public string UserName { get; set; } = "";
    public string Email { get; set; } = "";
    public string TemporaryPassword { get; set; } = "";

    public void Normalize()
    {
        UserName = (UserName ?? "").Trim();
        Email = (Email ?? "").Trim();
    }
}

public sealed class CreateUserValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.UserName).ValidUserName();
        RuleFor(x => x.Email).ValidEmail();
        RuleFor(x => x.TemporaryPassword).ValidTemporaryPassword();
    }
}

public sealed class CreateUserHandler(ISystemAdminService admin)
    : IRequestHandler<CreateUserRequest, UserDto>
{
    public Task<UserDto> Handle(CreateUserRequest request, CancellationToken cancellationToken) =>
        admin.CreateUserAsync(request, cancellationToken);
}

public sealed class UpdateUserRequest : IRequest<UserDto>, INormalizableRequest
{
    public string Id { get; set; } = "";
    public string UserName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Version { get; set; } = "";

    public void Normalize()
    {
        UserName = (UserName ?? "").Trim();
        Email = (Email ?? "").Trim();
    }
}

public sealed class UpdateUserValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserValidator()
    {
        RuleFor(x => x.Id).ValidId();
        RuleFor(x => x.Version).ValidId("กรุณาโหลดข้อมูลล่าสุดก่อนบันทึก");
        RuleFor(x => x.UserName).ValidUserName();
        RuleFor(x => x.Email).ValidEmail();
    }
}

public sealed class UpdateUserHandler(ISystemAdminService admin)
    : IRequestHandler<UpdateUserRequest, UserDto>
{
    public Task<UserDto> Handle(UpdateUserRequest request, CancellationToken cancellationToken) =>
        admin.UpdateUserAsync(request, cancellationToken);
}

public sealed class ResetTemporaryPasswordRequest : IRequest<UserDto>
{
    public string Id { get; set; } = "";
    public string TemporaryPassword { get; set; } = "";
    public string Version { get; set; } = "";
}

public sealed class ResetTemporaryPasswordValidator : AbstractValidator<ResetTemporaryPasswordRequest>
{
    public ResetTemporaryPasswordValidator()
    {
        RuleFor(x => x.Id).ValidId();
        RuleFor(x => x.Version).ValidId("กรุณาโหลดข้อมูลล่าสุดก่อนบันทึก");
        RuleFor(x => x.TemporaryPassword).ValidTemporaryPassword();
    }
}

public sealed class ResetTemporaryPasswordHandler(ISystemAdminService admin)
    : IRequestHandler<ResetTemporaryPasswordRequest, UserDto>
{
    public Task<UserDto> Handle(ResetTemporaryPasswordRequest request, CancellationToken cancellationToken) =>
        admin.ResetTemporaryPasswordAsync(request, cancellationToken);
}

public sealed record UnlockUserRequest(string Id, string Version) : IRequest<UserDto>;

public sealed class UnlockUserValidator : AbstractValidator<UnlockUserRequest>
{
    public UnlockUserValidator()
    {
        RuleFor(x => x.Id).ValidId();
        RuleFor(x => x.Version).ValidId("กรุณาโหลดข้อมูลล่าสุดก่อนบันทึก");
    }
}

public sealed class UnlockUserHandler(ISystemAdminService admin)
    : IRequestHandler<UnlockUserRequest, UserDto>
{
    public Task<UserDto> Handle(UnlockUserRequest request, CancellationToken cancellationToken) =>
        admin.UnlockUserAsync(request, cancellationToken);
}

public sealed record ChangeUserStatusRequest(string Id, string Version, bool IsActive) : IRequest<UserDto>;

public sealed class ChangeUserStatusValidator : AbstractValidator<ChangeUserStatusRequest>
{
    public ChangeUserStatusValidator()
    {
        RuleFor(x => x.Id).ValidId();
        RuleFor(x => x.Version).ValidId("กรุณาโหลดข้อมูลล่าสุดก่อนบันทึก");
    }
}

public sealed class ChangeUserStatusHandler(ISystemAdminService admin)
    : IRequestHandler<ChangeUserStatusRequest, UserDto>
{
    public Task<UserDto> Handle(ChangeUserStatusRequest request, CancellationToken cancellationToken) =>
        admin.ChangeUserStatusAsync(request, cancellationToken);
}

