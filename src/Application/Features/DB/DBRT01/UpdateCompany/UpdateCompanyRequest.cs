using clean.Application.Common.Behaviors;
using clean.Application.Common.Models;
using clean.Domain.Entities.Organization;
using FluentValidation;
using MediatR;

namespace clean.Application.Features.DB.DBRT01.UpdateCompany;

public sealed class UpdateCompanyRequest : IRequest<CompanyDto>, INormalizableRequest
{
    public string Id { get; set; } = "";
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public void Normalize() => Code = Company.NormalizeCode(Code);
}
public sealed class UpdateCompanyValidator : AbstractValidator<UpdateCompanyRequest>
{
    public UpdateCompanyValidator()
    {
        RuleFor(x => x.Id).Must(IdValue.IsValid).WithMessage("รหัสอ้างอิงไม่ถูกต้อง");
        RuleFor(x => x.Version).Must(IdValue.IsValid).WithMessage("กรุณาโหลดข้อมูลล่าสุดก่อนบันทึก");
        RuleFor(x => x.Code).NotEmpty().Matches(@"\A[A-Z0-9_-]{1,50}\z").WithMessage("รหัสต้องยาว 1–50 ตัว ใช้ A–Z, 0–9, - หรือ _");
        RuleFor(x => x.Name).NotEmpty().WithMessage("กรุณาระบุชื่อบริษัท")
            .MaximumLength(200).WithMessage("ชื่อบริษัทต้องไม่เกิน 200 ตัวอักษร");
    }
}
