using clean.Application.Common.Models;
using FluentValidation;

namespace clean.Application.Features.DB.Shared;

internal static class OrganizationMasterValidation
{
    public static IRuleBuilderOptions<T, string> ValidId<T>(this IRuleBuilder<T, string> rule, string message = "รหัสอ้างอิงไม่ถูกต้อง") =>
        rule.Must(IdValue.IsValid).WithMessage(message);

    public static IRuleBuilderOptions<T, string?> ValidOptionalId<T>(this IRuleBuilder<T, string?> rule, string message = "รหัสอ้างอิงไม่ถูกต้อง") =>
        rule.Must(x => string.IsNullOrEmpty(x) || IdValue.IsValid(x)).WithMessage(message);

    public static IRuleBuilderOptions<T, string> ValidCode<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("กรุณาระบุรหัส")
            .Matches(@"\A[A-Z0-9_-]{1,50}\z").WithMessage("รหัสต้องยาว 1–50 ตัว ใช้ A–Z, 0–9, - หรือ _");

    public static IRuleBuilderOptions<T, string> ValidName<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("กรุณาระบุชื่อ")
            .MaximumLength(200).WithMessage("ชื่อต้องไม่เกิน 200 ตัวอักษร");

    public static void ApplyPagingRules<T>(
        AbstractValidator<T> validator,
        System.Linq.Expressions.Expression<Func<T, string?>> keyword,
        System.Linq.Expressions.Expression<Func<T, int>> pageNumber,
        System.Linq.Expressions.Expression<Func<T, int>> pageSize,
        System.Linq.Expressions.Expression<Func<T, string>> sortField,
        System.Linq.Expressions.Expression<Func<T, string>> sortDirection,
        params string[] allowedSortFields)
    {
        validator.RuleFor(keyword).MaximumLength(200);
        validator.RuleFor(pageNumber).InclusiveBetween(1, 1000000);
        validator.RuleFor(pageSize).InclusiveBetween(1, 100);
        validator.RuleFor(sortField).Must(allowedSortFields.Contains);
        validator.RuleFor(sortDirection).Must(x => x is "asc" or "desc");
    }
}

