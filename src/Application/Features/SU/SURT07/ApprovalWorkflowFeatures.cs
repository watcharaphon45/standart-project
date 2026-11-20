using clean.Application.Common.Behaviors;
using clean.Application.Common.Interfaces;
using clean.Application.Common.Models;
using clean.Application.Features.DB.Shared;
using clean.Domain.Entities.Organization;
using FluentValidation;
using MediatR;

namespace clean.Application.Features.SU.SURT07;

public sealed record SearchApprovalWorkflowsRequest(
    string CompanyId,
    string? Keyword = null,
    bool? IsActive = null,
    int PageNumber = 1,
    int PageSize = 25,
    string SortField = "code",
    string SortDirection = "asc") : IRequest<Page<ApprovalWorkflowDto>>;

public sealed class SearchApprovalWorkflowsValidator : AbstractValidator<SearchApprovalWorkflowsRequest>
{
    public SearchApprovalWorkflowsValidator()
    {
        RuleFor(x => x.CompanyId).ValidId("กรุณาเลือกบริษัท");
        OrganizationMasterValidation.ApplyPagingRules(
            this,
            x => x.Keyword,
            x => x.PageNumber,
            x => x.PageSize,
            x => x.SortField,
            x => x.SortDirection,
            "code", "name", "documentType", "isActive");
    }
}

public sealed class SearchApprovalWorkflowsHandler(ISystemAdminService admin)
    : IRequestHandler<SearchApprovalWorkflowsRequest, Page<ApprovalWorkflowDto>>
{
    public Task<Page<ApprovalWorkflowDto>> Handle(SearchApprovalWorkflowsRequest request, CancellationToken cancellationToken) =>
        admin.SearchApprovalWorkflowsAsync(request, cancellationToken);
}

public sealed record GetApprovalWorkflowOptionsRequest(string CompanyId) : IRequest<ApprovalWorkflowOptionsDto>;

public sealed class GetApprovalWorkflowOptionsValidator : AbstractValidator<GetApprovalWorkflowOptionsRequest>
{
    public GetApprovalWorkflowOptionsValidator() => RuleFor(x => x.CompanyId).ValidId("กรุณาเลือกบริษัท");
}

public sealed class GetApprovalWorkflowOptionsHandler(ISystemAdminService admin)
    : IRequestHandler<GetApprovalWorkflowOptionsRequest, ApprovalWorkflowOptionsDto>
{
    public Task<ApprovalWorkflowOptionsDto> Handle(GetApprovalWorkflowOptionsRequest request, CancellationToken cancellationToken) =>
        admin.GetApprovalWorkflowOptionsAsync(request, cancellationToken);
}

public sealed record GetApprovalWorkflowDetailRequest(string Id) : IRequest<ApprovalWorkflowDetailDto>;

public sealed class GetApprovalWorkflowDetailValidator : AbstractValidator<GetApprovalWorkflowDetailRequest>
{
    public GetApprovalWorkflowDetailValidator() => RuleFor(x => x.Id).ValidId();
}

public sealed class GetApprovalWorkflowDetailHandler(ISystemAdminService admin)
    : IRequestHandler<GetApprovalWorkflowDetailRequest, ApprovalWorkflowDetailDto>
{
    public Task<ApprovalWorkflowDetailDto> Handle(GetApprovalWorkflowDetailRequest request, CancellationToken cancellationToken) =>
        admin.GetApprovalWorkflowDetailAsync(request, cancellationToken);
}

public sealed class ApprovalWorkflowStepInput
{
    public int StepOrder { get; set; }
    public string StepName { get; set; } = "";
    public string ApproverRoleId { get; set; } = "";
    public bool RequireDifferentActor { get; set; } = true;
}

public sealed class ApprovalWorkflowStepInputValidator : AbstractValidator<ApprovalWorkflowStepInput>
{
    public ApprovalWorkflowStepInputValidator()
    {
        RuleFor(x => x.StepOrder).GreaterThan(0).WithMessage("ลำดับขั้นต้องมากกว่า 0");
        RuleFor(x => x.StepName).NotEmpty().WithMessage("กรุณาระบุชื่อขั้นตอน")
            .MaximumLength(200).WithMessage("ชื่อขั้นตอนต้องไม่เกิน 200 ตัวอักษร");
        RuleFor(x => x.ApproverRoleId).ValidId("กรุณาเลือกบทบาทผู้อนุมัติ");
    }
}

internal static class ApprovalWorkflowValidation
{
    public static IRuleBuilderOptions<T, string> ValidDocumentType<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("กรุณาระบุประเภทเอกสาร")
            .Matches(@"\A[A-Z0-9_-]{1,50}\z").WithMessage("ประเภทเอกสารใช้ได้เฉพาะ A–Z, 0–9, - และ _");

    public static IRuleBuilderOptions<T, List<ApprovalWorkflowStepInput>> ValidStepsList<T>(
        this IRuleBuilder<T, List<ApprovalWorkflowStepInput>> rule) =>
        rule.NotEmpty().WithMessage("กรุณากำหนดขั้นตอนอนุมัติอย่างน้อย 1 ขั้น")
            .Must(list => list.Count <= 20).WithMessage("กำหนดขั้นตอนได้สูงสุด 20 ขั้น");

    public static void NormalizeSteps(IEnumerable<ApprovalWorkflowStepInput>? steps)
    {
        if (steps is null) return;
        foreach (var step in steps)
            step.StepName = (step.StepName ?? "").Trim();
    }
}

public sealed class CreateApprovalWorkflowRequest : IRequest<ApprovalWorkflowDetailDto>, INormalizableRequest
{
    public string CompanyId { get; set; } = "";
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string DocumentType { get; set; } = "";
    public List<ApprovalWorkflowStepInput> Steps { get; set; } = [];

    public void Normalize()
    {
        Code = Company.NormalizeCode(Code);
        Name = (Name ?? "").Trim();
        DocumentType = Company.NormalizeCode(DocumentType);
        ApprovalWorkflowValidation.NormalizeSteps(Steps);
    }
}

public sealed class CreateApprovalWorkflowValidator : AbstractValidator<CreateApprovalWorkflowRequest>
{
    public CreateApprovalWorkflowValidator()
    {
        RuleFor(x => x.CompanyId).ValidId("กรุณาเลือกบริษัท");
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).ValidName();
        RuleFor(x => x.DocumentType).ValidDocumentType();
        RuleFor(x => x.Steps).ValidStepsList();
        RuleForEach(x => x.Steps).SetValidator(new ApprovalWorkflowStepInputValidator());
    }
}

public sealed class CreateApprovalWorkflowHandler(ISystemAdminService admin)
    : IRequestHandler<CreateApprovalWorkflowRequest, ApprovalWorkflowDetailDto>
{
    public Task<ApprovalWorkflowDetailDto> Handle(CreateApprovalWorkflowRequest request, CancellationToken cancellationToken) =>
        admin.CreateApprovalWorkflowAsync(request, cancellationToken);
}

public sealed class UpdateApprovalWorkflowRequest : IRequest<ApprovalWorkflowDetailDto>, INormalizableRequest
{
    public string Id { get; set; } = "";
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string DocumentType { get; set; } = "";
    public List<ApprovalWorkflowStepInput> Steps { get; set; } = [];
    public string Version { get; set; } = "";

    public void Normalize()
    {
        Code = Company.NormalizeCode(Code);
        Name = (Name ?? "").Trim();
        DocumentType = Company.NormalizeCode(DocumentType);
        ApprovalWorkflowValidation.NormalizeSteps(Steps);
    }
}

public sealed class UpdateApprovalWorkflowValidator : AbstractValidator<UpdateApprovalWorkflowRequest>
{
    public UpdateApprovalWorkflowValidator()
    {
        RuleFor(x => x.Id).ValidId();
        RuleFor(x => x.Version).ValidId("กรุณาโหลดข้อมูลล่าสุดก่อนบันทึก");
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).ValidName();
        RuleFor(x => x.DocumentType).ValidDocumentType();
        RuleFor(x => x.Steps).ValidStepsList();
        RuleForEach(x => x.Steps).SetValidator(new ApprovalWorkflowStepInputValidator());
    }
}

public sealed class UpdateApprovalWorkflowHandler(ISystemAdminService admin)
    : IRequestHandler<UpdateApprovalWorkflowRequest, ApprovalWorkflowDetailDto>
{
    public Task<ApprovalWorkflowDetailDto> Handle(UpdateApprovalWorkflowRequest request, CancellationToken cancellationToken) =>
        admin.UpdateApprovalWorkflowAsync(request, cancellationToken);
}

public sealed record ChangeApprovalWorkflowStatusRequest(string Id, string Version, bool IsActive)
    : IRequest<ApprovalWorkflowDto>;

public sealed class ChangeApprovalWorkflowStatusValidator : AbstractValidator<ChangeApprovalWorkflowStatusRequest>
{
    public ChangeApprovalWorkflowStatusValidator()
    {
        RuleFor(x => x.Id).ValidId();
        RuleFor(x => x.Version).ValidId("กรุณาโหลดข้อมูลล่าสุดก่อนบันทึก");
    }
}

public sealed class ChangeApprovalWorkflowStatusHandler(ISystemAdminService admin)
    : IRequestHandler<ChangeApprovalWorkflowStatusRequest, ApprovalWorkflowDto>
{
    public Task<ApprovalWorkflowDto> Handle(ChangeApprovalWorkflowStatusRequest request, CancellationToken cancellationToken) =>
        admin.ChangeApprovalWorkflowStatusAsync(request, cancellationToken);
}
