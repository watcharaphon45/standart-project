using clean.Application.Common.Behaviors;
using clean.Application.Common.Models;
using clean.Domain.Entities.Organization;
using FluentValidation;
using MediatR;

namespace clean.Application.Features.DB.DBRT01.CreateCompany;

public sealed class CreateCompanyRequest : IRequest<CompanyDto>, INormalizableRequest
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public void Normalize() => Code = Company.NormalizeCode(Code);
}
public sealed class CreateCompanyValidator : AbstractValidator<CreateCompanyRequest>
{
    public CreateCompanyValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithMessage("กรุณาระบุรหัสบริษัท")
            .Matches(@"\A[A-Z0-9_-]{1,50}\z").WithMessage("รหัสต้องยาว 1–50 ตัว ใช้ A–Z, 0–9, - หรือ _");
        RuleFor(x => x.Name).NotEmpty().WithMessage("กรุณาระบุชื่อบริษัท")
            .MaximumLength(200).WithMessage("ชื่อบริษัทต้องไม่เกิน 200 ตัวอักษร");
    }
}
