using clean.Application.Common.Behaviors;
using clean.Application.Common.Interfaces;
using clean.Application.Common.Models;
using clean.Application.Features.DB.Shared;
using clean.Domain.Entities.Organization;
using FluentValidation;
using MediatR;

namespace clean.Application.Features.SU.SURT06;

internal static class DocumentSequenceValidation
{
    public static IRuleBuilderOptions<T, string> ValidPrefix<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("กรุณาระบุ Prefix เอกสาร")
            .Matches(@"\A[A-Z0-9_-]{1,20}\z")
            .WithMessage("Prefix ต้องยาว 1–20 ตัว ใช้ A–Z, 0–9, - หรือ _");

    public static IRuleBuilderOptions<T, string> ValidResetPolicy<T>(this IRuleBuilder<T, string> rule) =>
        rule.Must(x => x is "NONE" or "YEAR" or "YEAR_MONTH")
            .WithMessage("รูปแบบการรีเซ็ตเลขที่ไม่ถูกต้อง");
}

public sealed record SearchDocumentSequencesRequest(
    string CompanyId,
    string? Keyword = null,
    bool? IsActive = null,
    int PageNumber = 1,
    int PageSize = 25,
    string SortField = "code",
    string SortDirection = "asc") : IRequest<Page<DocumentSequenceDto>>;

public sealed class SearchDocumentSequencesValidator : AbstractValidator<SearchDocumentSequencesRequest>
{
    public SearchDocumentSequencesValidator()
    {
        RuleFor(x => x.CompanyId).ValidId("กรุณาเลือกบริษัท");
        OrganizationMasterValidation.ApplyPagingRules(
            this,
            x => x.Keyword,
            x => x.PageNumber,
            x => x.PageSize,
            x => x.SortField,
            x => x.SortDirection,
            "code", "name", "prefix", "isActive");
    }
}

public sealed class SearchDocumentSequencesHandler(ISystemAdminService admin)
    : IRequestHandler<SearchDocumentSequencesRequest, Page<DocumentSequenceDto>>
{
    public Task<Page<DocumentSequenceDto>> Handle(SearchDocumentSequencesRequest request, CancellationToken cancellationToken) =>
        admin.SearchDocumentSequencesAsync(request, cancellationToken);
}

public sealed record GetDocumentSequenceDetailRequest(string Id) : IRequest<DocumentSequenceDto>;

public sealed class GetDocumentSequenceDetailValidator : AbstractValidator<GetDocumentSequenceDetailRequest>
{
    public GetDocumentSequenceDetailValidator() => RuleFor(x => x.Id).ValidId();
}

public sealed class GetDocumentSequenceDetailHandler(ISystemAdminService admin)
    : IRequestHandler<GetDocumentSequenceDetailRequest, DocumentSequenceDto>
{
    public Task<DocumentSequenceDto> Handle(GetDocumentSequenceDetailRequest request, CancellationToken cancellationToken) =>
        admin.GetDocumentSequenceDetailAsync(request, cancellationToken);
}

public sealed class CreateDocumentSequenceRequest : IRequest<DocumentSequenceDto>, INormalizableRequest
{
    public string CompanyId { get; set; } = "";
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Prefix { get; set; } = "";
    public string ResetPolicy { get; set; } = "YEAR_MONTH";
    public int PaddingLength { get; set; } = 5;

    public void Normalize()
    {
        Code = Company.NormalizeCode(Code);
        Name = (Name ?? "").Trim();
        Prefix = Company.NormalizeCode(Prefix);
        ResetPolicy = Company.NormalizeCode(ResetPolicy);
    }
}

public sealed class CreateDocumentSequenceValidator : AbstractValidator<CreateDocumentSequenceRequest>
{
    public CreateDocumentSequenceValidator()
    {
        RuleFor(x => x.CompanyId).ValidId("กรุณาเลือกบริษัท");
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).ValidName();
        RuleFor(x => x.Prefix).ValidPrefix();
        RuleFor(x => x.ResetPolicy).ValidResetPolicy();
        RuleFor(x => x.PaddingLength).InclusiveBetween(3, 10).WithMessage("จำนวนหลักต้องอยู่ระหว่าง 3–10 หลัก");
    }
}

public sealed class CreateDocumentSequenceHandler(ISystemAdminService admin)
    : IRequestHandler<CreateDocumentSequenceRequest, DocumentSequenceDto>
{
    public Task<DocumentSequenceDto> Handle(CreateDocumentSequenceRequest request, CancellationToken cancellationToken) =>
        admin.CreateDocumentSequenceAsync(request, cancellationToken);
}

public sealed class UpdateDocumentSequenceRequest : IRequest<DocumentSequenceDto>, INormalizableRequest
{
    public string Id { get; set; } = "";
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Prefix { get; set; } = "";
    public string ResetPolicy { get; set; } = "YEAR_MONTH";
    public int PaddingLength { get; set; } = 5;
    public string Version { get; set; } = "";

    public void Normalize()
    {
        Code = Company.NormalizeCode(Code);
        Name = (Name ?? "").Trim();
        Prefix = Company.NormalizeCode(Prefix);
        ResetPolicy = Company.NormalizeCode(ResetPolicy);
    }
}

public sealed class UpdateDocumentSequenceValidator : AbstractValidator<UpdateDocumentSequenceRequest>
{
    public UpdateDocumentSequenceValidator()
    {
        RuleFor(x => x.Id).ValidId();
        RuleFor(x => x.Version).ValidId("กรุณาโหลดข้อมูลล่าสุดก่อนบันทึก");
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).ValidName();
        RuleFor(x => x.Prefix).ValidPrefix();
        RuleFor(x => x.ResetPolicy).ValidResetPolicy();
        RuleFor(x => x.PaddingLength).InclusiveBetween(3, 10).WithMessage("จำนวนหลักต้องอยู่ระหว่าง 3–10 หลัก");
    }
}

public sealed class UpdateDocumentSequenceHandler(ISystemAdminService admin)
    : IRequestHandler<UpdateDocumentSequenceRequest, DocumentSequenceDto>
{
    public Task<DocumentSequenceDto> Handle(UpdateDocumentSequenceRequest request, CancellationToken cancellationToken) =>
        admin.UpdateDocumentSequenceAsync(request, cancellationToken);
}

public sealed record ChangeDocumentSequenceStatusRequest(string Id, string Version, bool IsActive) : IRequest<DocumentSequenceDto>;

public sealed class ChangeDocumentSequenceStatusValidator : AbstractValidator<ChangeDocumentSequenceStatusRequest>
{
    public ChangeDocumentSequenceStatusValidator()
    {
        RuleFor(x => x.Id).ValidId();
        RuleFor(x => x.Version).ValidId("กรุณาโหลดข้อมูลล่าสุดก่อนบันทึก");
    }
}

public sealed class ChangeDocumentSequenceStatusHandler(ISystemAdminService admin)
    : IRequestHandler<ChangeDocumentSequenceStatusRequest, DocumentSequenceDto>
{
    public Task<DocumentSequenceDto> Handle(ChangeDocumentSequenceStatusRequest request, CancellationToken cancellationToken) =>
        admin.ChangeDocumentSequenceStatusAsync(request, cancellationToken);
}

public sealed record AllocateDocumentNumberRequest(string Id, string Version) : IRequest<DocumentNumberAllocationDto>;

public sealed class AllocateDocumentNumberValidator : AbstractValidator<AllocateDocumentNumberRequest>
{
    public AllocateDocumentNumberValidator()
    {
        RuleFor(x => x.Id).ValidId();
        RuleFor(x => x.Version).ValidId("กรุณาโหลดข้อมูลล่าสุดก่อนจองเลขที่เอกสาร");
    }
}

public sealed class AllocateDocumentNumberHandler(ISystemAdminService admin)
    : IRequestHandler<AllocateDocumentNumberRequest, DocumentNumberAllocationDto>
{
    public Task<DocumentNumberAllocationDto> Handle(AllocateDocumentNumberRequest request, CancellationToken cancellationToken) =>
        admin.AllocateDocumentNumberAsync(request, cancellationToken);
}

