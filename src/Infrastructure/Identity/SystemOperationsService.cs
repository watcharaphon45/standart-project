using System.Globalization;
using System.Text;
using clean.Application.Common.Models;
using clean.Application.Features.SU.SURT05;
using clean.Application.Features.SU.SURT06;
using clean.Application.Features.SU.SURT07;
using clean.Application.Features.SU.SURT08;
using clean.Application.Features.SU.SURT09;
using clean.Domain.Entities.Organization;
using clean.Domain.Entities.System;
using clean.Domain.Exceptions;
using clean.Domain.Security;
using Microsoft.EntityFrameworkCore;

namespace clean.Infrastructure.Identity;

public sealed partial class SystemAdminService
{
    private static readonly HashSet<string> GlobalAuditPrograms = new(
        ["AUTH", "BOOTSTRAP", "SURT01", "DBRT05", "DBRT07", "SURT05"],
        StringComparer.Ordinal);

    private static readonly (string Key, string DisplayName, string DataType, string Value, bool IsSensitive)[] DefaultGlobalSettings =
    [
        ("SESSION.IDLE_MINUTES", "ระยะเวลา Session หมดอายุเมื่อไม่มีการใช้งาน (นาที)", "INTEGER", "30", false),
        ("SESSION.ABSOLUTE_HOURS", "อายุ Session สูงสุดนับจาก Login (ชั่วโมง)", "INTEGER", "8", false),
        ("PASSWORD.MIN_LENGTH", "ความยาวรหัสผ่านขั้นต่ำ (ตัวอักษร)", "INTEGER", "15", false),
        ("AUTH.LOCKOUT_MAX_ATTEMPTS", "จำนวนครั้งที่อนุญาตให้กรอกรหัสผิดก่อนล็อก", "INTEGER", "5", false),
    ];

    public async Task<Page<MenuItemDto>> SearchMenuItemsAsync(
        SearchMenuItemsRequest request,
        CancellationToken cancellationToken)
    {
        await access.DemandGlobalAsync("SURT05.VIEW", cancellationToken);
        await EnsureMenusSeededAsync(cancellationToken);

        var query = db.MenuItems.AsNoTracking().AsQueryable();
        if (!string.IsNullOrEmpty(request.ModuleCode))
            query = query.Where(x => x.ModuleCode == request.ModuleCode);
        if (request.IsActive is not null)
            query = query.Where(x => x.IsActive == request.IsActive);
        var keyword = request.Keyword?.Trim();
        if (!string.IsNullOrEmpty(keyword))
        {
            var upper = keyword.ToUpperInvariant();
            query = query.Where(x =>
                x.ProgramCode.Contains(upper)
                || x.DisplayName.ToUpper().Contains(upper)
                || x.Route.ToUpper().Contains(upper));
        }

        var count = await query.CountAsync(cancellationToken);
        var ascending = request.SortDirection == "asc";
        var sorted = (request.SortField, ascending) switch
        {
            ("programCode", true) => query.OrderBy(x => x.ProgramCode),
            ("programCode", false) => query.OrderByDescending(x => x.ProgramCode),
            ("displayName", true) => query.OrderBy(x => x.DisplayName),
            ("displayName", false) => query.OrderByDescending(x => x.DisplayName),
            ("isActive", true) => query.OrderBy(x => x.IsActive),
            ("isActive", false) => query.OrderByDescending(x => x.IsActive),
            (_, true) => query.OrderBy(x => x.SortOrder),
            _ => query.OrderByDescending(x => x.SortOrder)
        };
        var rows = await sorted.ThenBy(x => x.Id)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);
        return new(rows.Select(ToMenuItemDto).ToArray(), count, request.PageNumber, request.PageSize);
    }

    public async Task<MenuItemDto> GetMenuItemDetailAsync(
        GetMenuItemDetailRequest request,
        CancellationToken cancellationToken)
    {
        await access.DemandGlobalAsync("SURT05.VIEW", cancellationToken);
        await EnsureMenusSeededAsync(cancellationToken);
        var id = IdValue.Parse(request.Id);
        var item = await db.MenuItems.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลเมนู", 404);
        return ToMenuItemDto(item);
    }

    public async Task<MenuItemDto> UpdateMenuItemAsync(
        UpdateMenuItemRequest request,
        CancellationToken cancellationToken)
    {
        await access.DemandGlobalAsync("SURT05.UPDATE", cancellationToken);
        var id = IdValue.Parse(request.Id);
        var expectedVersion = IdValue.Parse(request.Version);

        return await atomic.ExecuteAsync(async () =>
        {
            var item = await LockMenuItemAsync(id, cancellationToken)
                ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลเมนู", 404);
            OrganizationRules.CheckVersion(expectedVersion, item.Version);
            var before = ToMenuItemSnapshot(item);
            item.DisplayName = request.DisplayName.Trim();
            item.SortOrder = request.SortOrder;
            item.Version++;
            await db.SaveChangesAsync(cancellationToken);
            await audit.RecordOrganizationAsync(
                "SURT05", "UPDATE", null, item.Id, before, ToMenuItemSnapshot(item), cancellationToken);
            return ToMenuItemDto(item);
        }, cancellationToken);
    }

    public async Task<MenuItemDto> ChangeMenuItemStatusAsync(
        ChangeMenuItemStatusRequest request,
        CancellationToken cancellationToken)
    {
        var action = request.IsActive ? "ACTIVATE" : "DEACTIVATE";
        await access.DemandGlobalAsync("SURT05." + action, cancellationToken);
        var id = IdValue.Parse(request.Id);
        var expectedVersion = IdValue.Parse(request.Version);

        return await atomic.ExecuteAsync(async () =>
        {
            var item = await LockMenuItemAsync(id, cancellationToken)
                ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลเมนู", 404);
            OrganizationRules.CheckVersion(expectedVersion, item.Version);
            if (item.IsActive == request.IsActive)
                throw new RuleException("STATUS_UNCHANGED", "สถานะข้อมูลเปลี่ยนไปแล้ว กรุณาโหลดข้อมูลล่าสุด", 409);

            var before = ToMenuItemSnapshot(item);
            item.IsActive = request.IsActive;
            item.Version++;
            await db.SaveChangesAsync(cancellationToken);
            await audit.RecordOrganizationAsync(
                "SURT05", action, null, item.Id, before, ToMenuItemSnapshot(item), cancellationToken);
            return ToMenuItemDto(item);
        }, cancellationToken);
    }

    public async Task<Page<DocumentSequenceDto>> SearchDocumentSequencesAsync(
        SearchDocumentSequencesRequest request,
        CancellationToken cancellationToken)
    {
        var companyId = IdValue.Parse(request.CompanyId);
        await access.DemandCompanyAsync(companyId, "SURT06.VIEW", cancellationToken);

        var query = db.DocumentSequences.AsNoTracking().Where(x => x.CompanyId == companyId);
        if (request.IsActive is not null)
            query = query.Where(x => x.IsActive == request.IsActive);
        var keyword = request.Keyword?.Trim();
        if (!string.IsNullOrEmpty(keyword))
        {
            var upper = keyword.ToUpperInvariant();
            query = query.Where(x =>
                x.Code.Contains(upper)
                || x.Name.ToUpper().Contains(upper)
                || x.Prefix.Contains(upper));
        }

        var count = await query.CountAsync(cancellationToken);
        var ascending = request.SortDirection == "asc";
        var sorted = (request.SortField, ascending) switch
        {
            ("name", true) => query.OrderBy(x => x.Name),
            ("name", false) => query.OrderByDescending(x => x.Name),
            ("prefix", true) => query.OrderBy(x => x.Prefix),
            ("prefix", false) => query.OrderByDescending(x => x.Prefix),
            ("isActive", true) => query.OrderBy(x => x.IsActive),
            ("isActive", false) => query.OrderByDescending(x => x.IsActive),
            (_, true) => query.OrderBy(x => x.Code),
            _ => query.OrderByDescending(x => x.Code)
        };
        var rows = await sorted.ThenBy(x => x.Id)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);
        var now = clock.GetUtcNow();
        return new(rows.Select(x => ToDocumentSequenceDto(x, now)).ToArray(), count, request.PageNumber, request.PageSize);
    }

    public async Task<DocumentSequenceDto> GetDocumentSequenceDetailAsync(
        GetDocumentSequenceDetailRequest request,
        CancellationToken cancellationToken)
    {
        var id = IdValue.Parse(request.Id);
        var entity = await db.DocumentSequences.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลเลขที่เอกสาร", 404);
        await access.DemandCompanyAsync(entity.CompanyId, "SURT06.VIEW", cancellationToken);
        return ToDocumentSequenceDto(entity, clock.GetUtcNow());
    }

    public async Task<DocumentSequenceDto> CreateDocumentSequenceAsync(
        CreateDocumentSequenceRequest request,
        CancellationToken cancellationToken)
    {
        var companyId = IdValue.Parse(request.CompanyId);
        await access.DemandCompanyAsync(companyId, "SURT06.CREATE", cancellationToken);
        var (code, name) = OrganizationRules.NormalizeAndValidate(request.Code, request.Name);

        return await atomic.ExecuteAsync(async () =>
        {
            var company = await organizations.CompanyAsync(companyId, cancellationToken)
                ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลบริษัท", 404);
            if (!company.IsActive)
                throw new RuleException("PARENT_INACTIVE", "กรุณาเปิดใช้งานบริษัทก่อน", 409);

            if (await db.DocumentSequences.AnyAsync(x => x.CompanyId == companyId && x.Code == code, cancellationToken))
                throw new RuleException("DUPLICATE_CODE", "รหัสชุดเลขที่เอกสารนี้มีอยู่แล้ว", 409);

            var entity = new DocumentSequence
            {
                CompanyId = companyId,
                Code = code,
                Name = name,
                Prefix = request.Prefix,
                ResetPolicy = request.ResetPolicy,
                PaddingLength = request.PaddingLength,
                CurrentPeriod = CurrentPeriodKey(request.ResetPolicy, clock.GetUtcNow()),
                LastNumber = 0,
                IsActive = true,
                CodeLocked = false,
                Version = 1
            };
            db.DocumentSequences.Add(entity);
            await db.SaveChangesAsync(cancellationToken);
            await audit.RecordOrganizationAsync(
                "SURT06", "CREATE", entity.CompanyId, entity.Id, null, ToDocumentSequenceSnapshot(entity), cancellationToken);
            return ToDocumentSequenceDto(entity, clock.GetUtcNow());
        }, cancellationToken);
    }

    public async Task<DocumentSequenceDto> UpdateDocumentSequenceAsync(
        UpdateDocumentSequenceRequest request,
        CancellationToken cancellationToken)
    {
        var id = IdValue.Parse(request.Id);
        var expectedVersion = IdValue.Parse(request.Version);
        var target = await db.DocumentSequences.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.CompanyId })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลเลขที่เอกสาร", 404);

        await access.DemandCompanyAsync(target.CompanyId, "SURT06.UPDATE", cancellationToken);
        var (code, name) = OrganizationRules.NormalizeAndValidate(request.Code, request.Name);

        return await atomic.ExecuteAsync(async () =>
        {
            var entity = await LockDocumentSequenceAsync(id, cancellationToken)
                ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลเลขที่เอกสาร", 404);
            OrganizationRules.CheckVersion(expectedVersion, entity.Version);
            OrganizationRules.EnsureActiveForUpdate(entity.IsActive, "DOC_SEQUENCE_INACTIVE");
            OrganizationRules.EnsureCodeUnlocked(entity.CodeLocked, code, entity.Code);
            if (entity.CodeLocked && (request.Prefix != entity.Prefix || request.ResetPolicy != entity.ResetPolicy))
            {
                throw new RuleException(
                    "CODE_LOCKED",
                    "รหัส รูปแบบ Prefix และรอบรีเซ็ตถูกใช้ออกเลขเอกสารแล้ว จึงแก้ไขไม่ได้",
                    409);
            }

            if (await db.DocumentSequences.AnyAsync(
                x => x.Id != id && x.CompanyId == entity.CompanyId && x.Code == code,
                cancellationToken))
            {
                throw new RuleException("DUPLICATE_CODE", "รหัสชุดเลขที่เอกสารนี้มีอยู่แล้ว", 409);
            }

            var before = ToDocumentSequenceSnapshot(entity);
            entity.Code = code;
            entity.Name = name;
            entity.Prefix = request.Prefix;
            entity.ResetPolicy = request.ResetPolicy;
            entity.PaddingLength = request.PaddingLength;
            if (!entity.CodeLocked)
                entity.CurrentPeriod = CurrentPeriodKey(request.ResetPolicy, clock.GetUtcNow());
            entity.Version++;
            await db.SaveChangesAsync(cancellationToken);
            await audit.RecordOrganizationAsync(
                "SURT06", "UPDATE", entity.CompanyId, entity.Id, before, ToDocumentSequenceSnapshot(entity), cancellationToken);
            return ToDocumentSequenceDto(entity, clock.GetUtcNow());
        }, cancellationToken);
    }

    public async Task<DocumentSequenceDto> ChangeDocumentSequenceStatusAsync(
        ChangeDocumentSequenceStatusRequest request,
        CancellationToken cancellationToken)
    {
        var id = IdValue.Parse(request.Id);
        var expectedVersion = IdValue.Parse(request.Version);
        var action = request.IsActive ? "ACTIVATE" : "DEACTIVATE";
        var target = await db.DocumentSequences.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.CompanyId })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลเลขที่เอกสาร", 404);

        await access.DemandCompanyAsync(target.CompanyId, "SURT06." + action, cancellationToken);

        return await atomic.ExecuteAsync(async () =>
        {
            var entity = await LockDocumentSequenceAsync(id, cancellationToken)
                ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลเลขที่เอกสาร", 404);
            OrganizationRules.CheckVersion(expectedVersion, entity.Version);
            var company = await organizations.CompanyAsync(entity.CompanyId, cancellationToken);
            var parentActive = company?.IsActive ?? false;
            OrganizationRules.EnsureCanChangeStatus(request.IsActive, entity.IsActive, parentActive, false, false);

            var before = ToDocumentSequenceSnapshot(entity);
            entity.IsActive = request.IsActive;
            entity.Version++;
            await db.SaveChangesAsync(cancellationToken);
            await audit.RecordOrganizationAsync(
                "SURT06", action, entity.CompanyId, entity.Id, before, ToDocumentSequenceSnapshot(entity), cancellationToken);
            return ToDocumentSequenceDto(entity, clock.GetUtcNow());
        }, cancellationToken);
    }

    public async Task<DocumentNumberAllocationDto> AllocateDocumentNumberAsync(
        AllocateDocumentNumberRequest request,
        CancellationToken cancellationToken)
    {
        var id = IdValue.Parse(request.Id);
        var expectedVersion = IdValue.Parse(request.Version);
        var target = await db.DocumentSequences.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.CompanyId })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลเลขที่เอกสาร", 404);

        await access.DemandCompanyAsync(target.CompanyId, "SURT06.UPDATE", cancellationToken);

        return await atomic.ExecuteAsync(async () =>
        {
            var entity = await LockDocumentSequenceAsync(id, cancellationToken)
                ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลเลขที่เอกสาร", 404);
            OrganizationRules.CheckVersion(expectedVersion, entity.Version);
            var company = await organizations.CompanyAsync(entity.CompanyId, cancellationToken);
            if (company is null || !company.IsActive)
                throw new RuleException("PARENT_INACTIVE", "กรุณาเปิดใช้งานบริษัทก่อน", 409);
            OrganizationRules.EnsureActiveForUpdate(entity.IsActive, "DOC_SEQUENCE_INACTIVE");

            var before = ToDocumentSequenceSnapshot(entity);
            var periodKey = CurrentPeriodKey(entity.ResetPolicy, clock.GetUtcNow());
            if (!string.Equals(entity.CurrentPeriod, periodKey, StringComparison.Ordinal))
            {
                entity.CurrentPeriod = periodKey;
                entity.LastNumber = 0;
            }
            entity.LastNumber++;
            entity.CodeLocked = true;
            entity.Version++;
            var documentNumber = FormatDocumentNumber(entity.Prefix, periodKey, entity.LastNumber, entity.PaddingLength);

            await db.SaveChangesAsync(cancellationToken);
            await audit.RecordOrganizationAsync(
                "SURT06", "ALLOCATE", entity.CompanyId, entity.Id, before, ToDocumentSequenceSnapshot(entity), cancellationToken);

            return new DocumentNumberAllocationDto(
                entity.Id.ToString(CultureInfo.InvariantCulture),
                documentNumber,
                periodKey,
                entity.LastNumber,
                entity.Version.ToString(CultureInfo.InvariantCulture));
        }, cancellationToken);
    }

    public async Task<Page<ApprovalWorkflowDto>> SearchApprovalWorkflowsAsync(
        SearchApprovalWorkflowsRequest request,
        CancellationToken cancellationToken)
    {
        var companyId = IdValue.Parse(request.CompanyId);
        await access.DemandCompanyAsync(companyId, "SURT07.VIEW", cancellationToken);

        var query = db.ApprovalWorkflows.AsNoTracking().Where(x => x.CompanyId == companyId);
        if (request.IsActive is not null)
            query = query.Where(x => x.IsActive == request.IsActive);
        var keyword = request.Keyword?.Trim();
        if (!string.IsNullOrEmpty(keyword))
        {
            var upper = keyword.ToUpperInvariant();
            query = query.Where(x =>
                x.Code.Contains(upper)
                || x.Name.ToUpper().Contains(upper)
                || x.DocumentType.Contains(upper));
        }

        var count = await query.CountAsync(cancellationToken);
        var ascending = request.SortDirection == "asc";
        var sorted = (request.SortField, ascending) switch
        {
            ("name", true) => query.OrderBy(x => x.Name),
            ("name", false) => query.OrderByDescending(x => x.Name),
            ("documentType", true) => query.OrderBy(x => x.DocumentType),
            ("documentType", false) => query.OrderByDescending(x => x.DocumentType),
            ("isActive", true) => query.OrderBy(x => x.IsActive),
            ("isActive", false) => query.OrderByDescending(x => x.IsActive),
            (_, true) => query.OrderBy(x => x.Code),
            _ => query.OrderByDescending(x => x.Code)
        };
        var rows = await sorted.ThenBy(x => x.Id)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var ids = rows.Select(x => x.Id).ToArray();
        var stepCounts = ids.Length == 0
            ? new Dictionary<long, int>()
            : await db.ApprovalWorkflowSteps.AsNoTracking()
                .Where(x => ids.Contains(x.WorkflowId))
                .GroupBy(x => x.WorkflowId)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);

        var dtos = rows.Select(w => new ApprovalWorkflowDto(
            w.Id.ToString(CultureInfo.InvariantCulture),
            w.CompanyId.ToString(CultureInfo.InvariantCulture),
            w.Code,
            w.Name,
            w.DocumentType,
            stepCounts.GetValueOrDefault(w.Id),
            w.IsActive,
            w.CodeLocked,
            w.Version.ToString(CultureInfo.InvariantCulture))).ToArray();

        return new(dtos, count, request.PageNumber, request.PageSize);
    }

    public async Task<ApprovalWorkflowOptionsDto> GetApprovalWorkflowOptionsAsync(
        GetApprovalWorkflowOptionsRequest request,
        CancellationToken cancellationToken)
    {
        var companyId = IdValue.Parse(request.CompanyId);
        await access.DemandCompanyAsync(companyId, "SURT07.VIEW", cancellationToken);

        var roles = await db.Roles.AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .OrderBy(x => x.NormalizedName)
            .Select(x => new LookupOptionDto(
                x.Id.ToString(CultureInfo.InvariantCulture),
                x.Name ?? "",
                string.IsNullOrWhiteSpace(x.Description) ? x.Name ?? "" : x.Description,
                x.IsActive))
            .ToArrayAsync(cancellationToken);
        return new ApprovalWorkflowOptionsDto(roles);
    }

    public async Task<ApprovalWorkflowDetailDto> GetApprovalWorkflowDetailAsync(
        GetApprovalWorkflowDetailRequest request,
        CancellationToken cancellationToken)
    {
        var id = IdValue.Parse(request.Id);
        var workflow = await db.ApprovalWorkflows.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลขั้นตอนอนุมัติ", 404);
        await access.DemandCompanyAsync(workflow.CompanyId, "SURT07.VIEW", cancellationToken);
        return await BuildApprovalWorkflowDetailAsync(workflow, cancellationToken);
    }

    public async Task<ApprovalWorkflowDetailDto> CreateApprovalWorkflowAsync(
        CreateApprovalWorkflowRequest request,
        CancellationToken cancellationToken)
    {
        var companyId = IdValue.Parse(request.CompanyId);
        await access.DemandCompanyAsync(companyId, "SURT07.CREATE", cancellationToken);
        var (code, name) = OrganizationRules.NormalizeAndValidate(request.Code, request.Name);
        var orderedSteps = ValidateWorkflowStepsOrderAndActorRule(request.Steps);
        await ValidateWorkflowRolesAsync(companyId, orderedSteps.Select(x => IdValue.Parse(x.ApproverRoleId)), cancellationToken);

        var created = await atomic.ExecuteAsync(async () =>
        {
            var company = await organizations.CompanyAsync(companyId, cancellationToken)
                ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลบริษัท", 404);
            if (!company.IsActive)
                throw new RuleException("PARENT_INACTIVE", "กรุณาเปิดใช้งานบริษัทก่อน", 409);

            if (await db.ApprovalWorkflows.AnyAsync(x => x.CompanyId == companyId && x.Code == code, cancellationToken))
                throw new RuleException("DUPLICATE_CODE", "รหัสขั้นตอนอนุมัตินี้มีอยู่แล้ว", 409);

            var workflow = new ApprovalWorkflow
            {
                CompanyId = companyId,
                Code = code,
                Name = name,
                DocumentType = request.DocumentType,
                IsActive = true,
                CodeLocked = false,
                Version = 1
            };
            db.ApprovalWorkflows.Add(workflow);
            await db.SaveChangesAsync(cancellationToken);

            await LockWorkflowRolesAsync(orderedSteps.Select(x => IdValue.Parse(x.ApproverRoleId)), cancellationToken);
            var stepEntities = orderedSteps.Select(s => new ApprovalWorkflowStep
            {
                WorkflowId = workflow.Id,
                StepOrder = s.StepOrder,
                StepName = s.StepName,
                ApproverRoleId = IdValue.Parse(s.ApproverRoleId),
                RequireDifferentActor = s.RequireDifferentActor
            }).ToArray();
            db.ApprovalWorkflowSteps.AddRange(stepEntities);
            await db.SaveChangesAsync(cancellationToken);

            await audit.RecordOrganizationAsync(
                "SURT07",
                "CREATE",
                workflow.CompanyId,
                workflow.Id,
                null,
                ToWorkflowSnapshot(workflow, stepEntities),
                cancellationToken);
            return workflow;
        }, cancellationToken);

        return await BuildApprovalWorkflowDetailAsync(created, cancellationToken);
    }

    public async Task<ApprovalWorkflowDetailDto> UpdateApprovalWorkflowAsync(
        UpdateApprovalWorkflowRequest request,
        CancellationToken cancellationToken)
    {
        var id = IdValue.Parse(request.Id);
        var expectedVersion = IdValue.Parse(request.Version);
        var target = await db.ApprovalWorkflows.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.CompanyId })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลขั้นตอนอนุมัติ", 404);

        await access.DemandCompanyAsync(target.CompanyId, "SURT07.UPDATE", cancellationToken);
        var (code, name) = OrganizationRules.NormalizeAndValidate(request.Code, request.Name);
        var orderedSteps = ValidateWorkflowStepsOrderAndActorRule(request.Steps);
        await ValidateWorkflowRolesAsync(target.CompanyId, orderedSteps.Select(x => IdValue.Parse(x.ApproverRoleId)), cancellationToken);

        var updated = await atomic.ExecuteAsync(async () =>
        {
            var workflow = await LockApprovalWorkflowAsync(id, cancellationToken)
                ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลขั้นตอนอนุมัติ", 404);
            OrganizationRules.CheckVersion(expectedVersion, workflow.Version);
            OrganizationRules.EnsureActiveForUpdate(workflow.IsActive, "WORKFLOW_INACTIVE");
            OrganizationRules.EnsureCodeUnlocked(workflow.CodeLocked, code, workflow.Code);

            if (await db.ApprovalWorkflows.AnyAsync(
                x => x.Id != id && x.CompanyId == workflow.CompanyId && x.Code == code,
                cancellationToken))
            {
                throw new RuleException("DUPLICATE_CODE", "รหัสขั้นตอนอนุมัตินี้มีอยู่แล้ว", 409);
            }

            var existingSteps = await db.ApprovalWorkflowSteps
                .Where(x => x.WorkflowId == id)
                .OrderBy(x => x.StepOrder)
                .ToListAsync(cancellationToken);
            var before = ToWorkflowSnapshot(workflow, existingSteps);

            await LockWorkflowRolesAsync(orderedSteps.Select(x => IdValue.Parse(x.ApproverRoleId)), cancellationToken);
            if (existingSteps.Count > 0)
            {
                db.ApprovalWorkflowSteps.RemoveRange(existingSteps);
                await db.SaveChangesAsync(cancellationToken);
            }

            workflow.Code = code;
            workflow.Name = name;
            workflow.DocumentType = request.DocumentType;
            workflow.Version++;

            var nextSteps = orderedSteps.Select(s => new ApprovalWorkflowStep
            {
                WorkflowId = workflow.Id,
                StepOrder = s.StepOrder,
                StepName = s.StepName,
                ApproverRoleId = IdValue.Parse(s.ApproverRoleId),
                RequireDifferentActor = s.RequireDifferentActor
            }).ToArray();
            db.ApprovalWorkflowSteps.AddRange(nextSteps);
            await db.SaveChangesAsync(cancellationToken);

            await audit.RecordOrganizationAsync(
                "SURT07",
                "UPDATE",
                workflow.CompanyId,
                workflow.Id,
                before,
                ToWorkflowSnapshot(workflow, nextSteps),
                cancellationToken);
            return workflow;
        }, cancellationToken);

        return await BuildApprovalWorkflowDetailAsync(updated, cancellationToken);
    }

    public async Task<ApprovalWorkflowDto> ChangeApprovalWorkflowStatusAsync(
        ChangeApprovalWorkflowStatusRequest request,
        CancellationToken cancellationToken)
    {
        var id = IdValue.Parse(request.Id);
        var expectedVersion = IdValue.Parse(request.Version);
        var action = request.IsActive ? "ACTIVATE" : "DEACTIVATE";
        var target = await db.ApprovalWorkflows.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.CompanyId })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลขั้นตอนอนุมัติ", 404);

        await access.DemandCompanyAsync(target.CompanyId, "SURT07." + action, cancellationToken);

        return await atomic.ExecuteAsync(async () =>
        {
            var workflow = await LockApprovalWorkflowAsync(id, cancellationToken)
                ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลขั้นตอนอนุมัติ", 404);
            OrganizationRules.CheckVersion(expectedVersion, workflow.Version);
            var company = await organizations.CompanyAsync(workflow.CompanyId, cancellationToken);
            var parentActive = company?.IsActive ?? false;
            OrganizationRules.EnsureCanChangeStatus(request.IsActive, workflow.IsActive, parentActive, false, false);

            var steps = await db.ApprovalWorkflowSteps.AsNoTracking()
                .Where(x => x.WorkflowId == id)
                .OrderBy(x => x.StepOrder)
                .ToListAsync(cancellationToken);
            if (request.IsActive)
            {
                await ValidateWorkflowRolesAsync(
                    workflow.CompanyId,
                    steps.Select(s => s.ApproverRoleId),
                    cancellationToken);
            }

            var before = ToWorkflowSnapshot(workflow, steps);
            workflow.IsActive = request.IsActive;
            workflow.Version++;
            await db.SaveChangesAsync(cancellationToken);
            await audit.RecordOrganizationAsync(
                "SURT07",
                action,
                workflow.CompanyId,
                workflow.Id,
                before,
                ToWorkflowSnapshot(workflow, steps),
                cancellationToken);

            return new ApprovalWorkflowDto(
                workflow.Id.ToString(CultureInfo.InvariantCulture),
                workflow.CompanyId.ToString(CultureInfo.InvariantCulture),
                workflow.Code,
                workflow.Name,
                workflow.DocumentType,
                steps.Count,
                workflow.IsActive,
                workflow.CodeLocked,
                workflow.Version.ToString(CultureInfo.InvariantCulture));
        }, cancellationToken);
    }

    public async Task<Page<SystemSettingDto>> SearchSystemSettingsAsync(
        SearchSystemSettingsRequest request,
        CancellationToken cancellationToken)
    {
        long? companyId = string.IsNullOrEmpty(request.CompanyId) ? null : IdValue.Parse(request.CompanyId);
        await DemandSettingAccessAsync(companyId, "VIEW", cancellationToken);
        if (companyId is null)
            await EnsureGlobalSettingsSeededAsync(cancellationToken);

        var query = db.SystemSettings.AsNoTracking().Where(x => x.CompanyId == companyId);
        var keyword = request.Keyword?.Trim();
        if (!string.IsNullOrEmpty(keyword))
        {
            var upper = keyword.ToUpperInvariant();
            query = query.Where(x =>
                x.Key.Contains(upper) || x.DisplayName.ToUpper().Contains(upper));
        }

        var count = await query.CountAsync(cancellationToken);
        var ascending = request.SortDirection == "asc";
        var sorted = (request.SortField, ascending) switch
        {
            ("displayName", true) => query.OrderBy(x => x.DisplayName),
            ("displayName", false) => query.OrderByDescending(x => x.DisplayName),
            ("dataType", true) => query.OrderBy(x => x.DataType),
            ("dataType", false) => query.OrderByDescending(x => x.DataType),
            (_, true) => query.OrderBy(x => x.Key),
            _ => query.OrderByDescending(x => x.Key)
        };
        var rows = await sorted.ThenBy(x => x.Id)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);
        return new(rows.Select(ToSystemSettingDto).ToArray(), count, request.PageNumber, request.PageSize);
    }

    public async Task<SystemSettingDto> GetSystemSettingDetailAsync(
        GetSystemSettingDetailRequest request,
        CancellationToken cancellationToken)
    {
        var id = IdValue.Parse(request.Id);
        var entity = await db.SystemSettings.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลการตั้งค่าระบบ", 404);
        await DemandSettingAccessAsync(entity.CompanyId, "VIEW", cancellationToken);
        return ToSystemSettingDto(entity);
    }

    public async Task<SystemSettingDto> CreateSystemSettingAsync(
        CreateSystemSettingRequest request,
        CancellationToken cancellationToken)
    {
        long? companyId = string.IsNullOrEmpty(request.CompanyId) ? null : IdValue.Parse(request.CompanyId);
        await DemandSettingAccessAsync(companyId, "CREATE", cancellationToken);
        var validatedValue = NormalizeAndValidateSettingValue(request.DataType, request.Value);

        return await atomic.ExecuteAsync(async () =>
        {
            if (companyId is long cid)
            {
                var company = await organizations.CompanyAsync(cid, cancellationToken)
                    ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลบริษัท", 404);
                if (!company.IsActive)
                    throw new RuleException("PARENT_INACTIVE", "กรุณาเปิดใช้งานบริษัทก่อน", 409);
            }

            if (await db.SystemSettings.AnyAsync(x => x.CompanyId == companyId && x.Key == request.Key, cancellationToken))
                throw new RuleException("DUPLICATE_CODE", "คีย์ตั้งค่านี้มีอยู่ในขอบเขตเดียวกันแล้ว", 409);

            var entity = new SystemSetting
            {
                CompanyId = companyId,
                Key = request.Key,
                DisplayName = request.DisplayName,
                DataType = request.DataType,
                Value = validatedValue,
                IsSensitive = request.IsSensitive,
                Version = 1
            };
            db.SystemSettings.Add(entity);
            await db.SaveChangesAsync(cancellationToken);
            await audit.RecordOrganizationAsync(
                "SURT08", "CREATE", entity.CompanyId, entity.Id, null, ToSystemSettingSnapshot(entity), cancellationToken);
            return ToSystemSettingDto(entity);
        }, cancellationToken);
    }

    public async Task<SystemSettingDto> UpdateSystemSettingAsync(
        UpdateSystemSettingRequest request,
        CancellationToken cancellationToken)
    {
        var id = IdValue.Parse(request.Id);
        var expectedVersion = IdValue.Parse(request.Version);
        var target = await db.SystemSettings.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.CompanyId })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลการตั้งค่าระบบ", 404);

        await DemandSettingAccessAsync(target.CompanyId, "UPDATE", cancellationToken);

        return await atomic.ExecuteAsync(async () =>
        {
            var entity = await LockSystemSettingAsync(id, cancellationToken)
                ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลการตั้งค่าระบบ", 404);
            OrganizationRules.CheckVersion(expectedVersion, entity.Version);
            var before = ToSystemSettingSnapshot(entity);

            var nextValue = entity.IsSensitive && string.IsNullOrEmpty(request.Value)
                ? entity.Value
                : NormalizeAndValidateSettingValue(entity.DataType, request.Value);

            entity.DisplayName = request.DisplayName;
            entity.Value = nextValue;
            entity.Version++;
            await db.SaveChangesAsync(cancellationToken);
            await audit.RecordOrganizationAsync(
                "SURT08", "UPDATE", entity.CompanyId, entity.Id, before, ToSystemSettingSnapshot(entity), cancellationToken);
            return ToSystemSettingDto(entity);
        }, cancellationToken);
    }

    public async Task<Page<AuditEventItemDto>> SearchAuditLogsAsync(
        SearchAuditLogsRequest request,
        CancellationToken cancellationToken)
    {
        var query = await BuildScopedAuditQueryAsync(
            request.CompanyId,
            request.ProgramCode,
            request.Action,
            request.Keyword,
            request.SortField,
            request.SortDirection,
            "SURT09.VIEW",
            cancellationToken);

        var count = await query.CountAsync(cancellationToken);
        var rows = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToArrayAsync(cancellationToken);
        return new(rows, count, request.PageNumber, request.PageSize);
    }

    public async Task<AuditEventDetailDto> GetAuditLogDetailAsync(
        GetAuditLogDetailRequest request,
        CancellationToken cancellationToken)
    {
        var id = IdValue.Parse(request.Id);
        var row = await (from ev in db.AuditEvents.AsNoTracking()
                         join user in db.Users.AsNoTracking() on ev.ActorUserId equals user.Id into userGroup
                         from user in userGroup.DefaultIfEmpty()
                         where ev.Id == id
                         select new
                         {
                             Event = ev,
                             ActorUserName = user != null ? (user.UserName ?? "") : ""
                         }).SingleOrDefaultAsync(cancellationToken)
            ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลประวัติการใช้งาน", 404);

        var evItem = row.Event;
        if (evItem.CompanyId is null || GlobalAuditPrograms.Contains(evItem.ProgramCode))
        {
            await access.DemandGlobalAsync("SURT09.VIEW", cancellationToken);
        }
        else
        {
            var scope = await access.GetScopeFilterAsync(evItem.CompanyId.Value, "SURT09.VIEW", cancellationToken);
            if (!scope.FullCompany && !await IsAuditEventWithinNarrowScopeAsync(evItem, scope, cancellationToken))
                throw new RuleException("FORBIDDEN", "คุณไม่มีสิทธิ์ดูประวัติรายการนี้นอกขอบเขตของคุณ", 403);
        }

        return new AuditEventDetailDto(
            evItem.Id.ToString(CultureInfo.InvariantCulture),
            evItem.OccurredAtUtc,
            evItem.ActorUserId?.ToString(CultureInfo.InvariantCulture),
            row.ActorUserName,
            evItem.CompanyId?.ToString(CultureInfo.InvariantCulture),
            evItem.EntityId?.ToString(CultureInfo.InvariantCulture),
            evItem.ProgramCode,
            evItem.Action,
            evItem.Result,
            evItem.TraceId,
            evItem.Before,
            evItem.After);
    }

    public async Task<AuditExportDto> ExportAuditLogsAsync(
        ExportAuditLogsRequest request,
        CancellationToken cancellationToken)
    {
        var query = await BuildScopedAuditQueryAsync(
            request.CompanyId,
            request.ProgramCode,
            request.Action,
            request.Keyword,
            request.SortField,
            request.SortDirection,
            "SURT09.EXPORT",
            cancellationToken);

        var totalCount = await query.CountAsync(cancellationToken);
        var rows = await query.Take(5000).ToArrayAsync(cancellationToken);
        var builder = new StringBuilder();
        builder.AppendLine("Id,OccurredAtUtc,ActorUserId,ActorUserName,CompanyId,EntityId,ProgramCode,Action,Result,TraceId");
        foreach (var row in rows)
        {
            builder.Append(EscapeCsvCell(row.Id)).Append(',')
                .Append(EscapeCsvCell(row.OccurredAtUtc.ToString("O", CultureInfo.InvariantCulture))).Append(',')
                .Append(EscapeCsvCell(row.ActorUserId)).Append(',')
                .Append(EscapeCsvCell(row.ActorUserName)).Append(',')
                .Append(EscapeCsvCell(row.CompanyId)).Append(',')
                .Append(EscapeCsvCell(row.EntityId)).Append(',')
                .Append(EscapeCsvCell(row.ProgramCode)).Append(',')
                .Append(EscapeCsvCell(row.Action)).Append(',')
                .Append(EscapeCsvCell(row.Result)).Append(',')
                .Append(EscapeCsvCell(row.TraceId)).AppendLine();
        }

        var stamp = clock.GetUtcNow().UtcDateTime.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        return new AuditExportDto($"audit-log-{stamp}.csv", "text/csv; charset=utf-8", builder.ToString(), totalCount);
    }

    private async Task EnsureMenusSeededAsync(CancellationToken cancellationToken)
    {
        var existingCodes = await db.MenuItems.AsNoTracking()
            .Select(x => x.ProgramCode)
            .ToArrayAsync(cancellationToken);
        var existingSet = existingCodes.ToHashSet(StringComparer.Ordinal);
        var missing = ProgramRegistry.Programs
            .Select((program, index) => new { program, sortOrder = index + 1 })
            .Where(x => x.program.Implemented && !existingSet.Contains(x.program.Code))
            .Select(x => new MenuItem
            {
                ModuleCode = x.program.Code[..2],
                ProgramCode = x.program.Code,
                DisplayName = x.program.Name,
                Route = x.program.Route,
                RequiredPermission = x.program.Code + ".VIEW",
                SortOrder = x.sortOrder,
                IsActive = true,
                Version = 1
            }).ToArray();

        if (missing.Length == 0) return;
        db.MenuItems.AddRange(missing);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureGlobalSettingsSeededAsync(CancellationToken cancellationToken)
    {
        if (await db.SystemSettings.AsNoTracking().AnyAsync(x => x.CompanyId == null, cancellationToken))
            return;

        db.SystemSettings.AddRange(DefaultGlobalSettings.Select(x => new SystemSetting
        {
            CompanyId = null,
            Key = x.Key,
            DisplayName = x.DisplayName,
            DataType = x.DataType,
            Value = x.Value,
            IsSensitive = x.IsSensitive,
            Version = 1
        }));
        await db.SaveChangesAsync(cancellationToken);
    }

    private Task DemandSettingAccessAsync(long? companyId, string action, CancellationToken cancellationToken) =>
        companyId is long cid
            ? access.DemandCompanyAsync(cid, "SURT08." + action, cancellationToken)
            : access.DemandGlobalAsync("SURT08." + action, cancellationToken);

    private Task<MenuItem?> LockMenuItemAsync(long id, CancellationToken cancellationToken) =>
        db.MenuItems.FromSqlInterpolated($"""SELECT * FROM "SuMenuItem" WHERE "Id" = {id} FOR UPDATE""")
            .SingleOrDefaultAsync(cancellationToken);

    private Task<DocumentSequence?> LockDocumentSequenceAsync(long id, CancellationToken cancellationToken) =>
        db.DocumentSequences.FromSqlInterpolated($"""SELECT * FROM "SuDocumentSequence" WHERE "Id" = {id} FOR UPDATE""")
            .SingleOrDefaultAsync(cancellationToken);

    private Task<ApprovalWorkflow?> LockApprovalWorkflowAsync(long id, CancellationToken cancellationToken) =>
        db.ApprovalWorkflows.FromSqlInterpolated($"""SELECT * FROM "SuApprovalWorkflow" WHERE "Id" = {id} FOR UPDATE""")
            .SingleOrDefaultAsync(cancellationToken);

    private Task<SystemSetting?> LockSystemSettingAsync(long id, CancellationToken cancellationToken) =>
        db.SystemSettings.FromSqlInterpolated($"""SELECT * FROM "SuSystemSetting" WHERE "Id" = {id} FOR UPDATE""")
            .SingleOrDefaultAsync(cancellationToken);

    private static List<ApprovalWorkflowStepInput> ValidateWorkflowStepsOrderAndActorRule(
        IEnumerable<ApprovalWorkflowStepInput> steps)
    {
        var ordered = steps.OrderBy(x => x.StepOrder).ToList();
        for (var i = 0; i < ordered.Count; i++)
        {
            if (ordered[i].StepOrder != i + 1)
                throw new RuleException("INVALID_WORKFLOW_STEPS", "ลำดับขั้นตอนอนุมัติต้องเรียงต่อเนื่อง 1 ถึง N", 400);
            if (i > 0
                && !ordered[i].RequireDifferentActor
                && ordered[i].ApproverRoleId == ordered[i - 1].ApproverRoleId)
            {
                throw new RuleException(
                    "INVALID_WORKFLOW_STEPS",
                    "ขั้นตอนต่อเนื่องที่ใช้บทบาทเดียวกันต้องเปิดการแยกผู้ปฏิบัติ (RequireDifferentActor)",
                    400);
            }
        }
        return ordered;
    }

    private async Task ValidateWorkflowRolesAsync(
        long companyId,
        IEnumerable<long> roleIds,
        CancellationToken cancellationToken)
    {
        var distinct = roleIds.Distinct().ToArray();
        var roles = await db.Roles.AsNoTracking()
            .Where(x => distinct.Contains(x.Id))
            .Select(x => new { x.Id, x.CompanyId, x.IsActive })
            .ToListAsync(cancellationToken);
        if (roles.Count != distinct.Length)
            throw new RuleException("ROLE_NOT_FOUND", "ไม่พบบทบาทผู้อนุมัติที่เลือก", 404);
        if (roles.Any(r => r.CompanyId != companyId))
            throw new RuleException("FORBIDDEN", "ไม่สามารถเลือกบทบาทข้ามบริษัทหรือบทบาทส่วนกลางในขั้นตอนอนุมัติของบริษัทได้", 403);
        if (roles.Any(r => !r.IsActive))
            throw new RuleException("ROLE_INACTIVE", "กรุณาเลือกเฉพาะบทบาทที่เปิดใช้งานอยู่", 409);
    }

    private async Task LockWorkflowRolesAsync(IEnumerable<long> roleIds, CancellationToken cancellationToken)
    {
        foreach (var roleId in roleIds.Distinct())
        {
            var role = await LockRoleAsync(roleId, cancellationToken)
                ?? throw new RuleException("ROLE_NOT_FOUND", "ไม่พบบทบาทผู้อนุมัติที่เลือก", 404);
            if (!role.CodeLocked)
            {
                role.CodeLocked = true;
                role.Version++;
            }
        }
    }

    private async Task<ApprovalWorkflowDetailDto> BuildApprovalWorkflowDetailAsync(
        ApprovalWorkflow workflow,
        CancellationToken cancellationToken)
    {
        var steps = await (from step in db.ApprovalWorkflowSteps.AsNoTracking()
                           join role in db.Roles.AsNoTracking() on step.ApproverRoleId equals role.Id
                           where step.WorkflowId == workflow.Id
                           orderby step.StepOrder
                           select new ApprovalWorkflowStepDto(
                               step.Id.ToString(CultureInfo.InvariantCulture),
                               step.StepOrder,
                               step.StepName,
                               role.Id.ToString(CultureInfo.InvariantCulture),
                               role.Name ?? "",
                               string.IsNullOrWhiteSpace(role.Description) ? role.Name ?? "" : role.Description,
                               step.RequireDifferentActor)).ToArrayAsync(cancellationToken);

        return new ApprovalWorkflowDetailDto(
            workflow.Id.ToString(CultureInfo.InvariantCulture),
            workflow.CompanyId.ToString(CultureInfo.InvariantCulture),
            workflow.Code,
            workflow.Name,
            workflow.DocumentType,
            workflow.IsActive,
            workflow.CodeLocked,
            workflow.Version.ToString(CultureInfo.InvariantCulture),
            steps);
    }

    private async Task<IQueryable<AuditEventItemDto>> BuildScopedAuditQueryAsync(
        string? companyIdStr,
        string? programCode,
        string? action,
        string? keyword,
        string sortField,
        string sortDirection,
        string permission,
        CancellationToken cancellationToken)
    {
        var events = db.AuditEvents.AsNoTracking();
        if (string.IsNullOrEmpty(companyIdStr))
        {
            await access.DemandGlobalAsync(permission, cancellationToken);
        }
        else
        {
            var companyId = IdValue.Parse(companyIdStr);
            var scope = await access.GetScopeFilterAsync(companyId, permission, cancellationToken);
            events = events.Where(x => x.CompanyId == companyId && !GlobalAuditPrograms.Contains(x.ProgramCode));
            if (!scope.FullCompany)
            {
                var branchIds = scope.BranchIds.ToArray();
                var divIds = (await organizations.DivisionIdsByBranchesAsync(companyId, scope.BranchIds, cancellationToken)).ToArray();
                var branchDeptIds = await organizations.DepartmentIdsByBranchesAsync(companyId, scope.BranchIds, cancellationToken);
                var deptIds = branchDeptIds.Concat(scope.DepartmentIds).Distinct().ToArray();

                events = events.Where(x =>
                    (x.ProgramCode == "DBRT02" && x.EntityId != null && branchIds.Contains(x.EntityId.Value))
                    || (x.ProgramCode == "DBRT04" && x.EntityId != null && divIds.Contains(x.EntityId.Value))
                    || (x.ProgramCode == "DBRT03" && x.EntityId != null && deptIds.Contains(x.EntityId.Value)));
            }
        }

        if (!string.IsNullOrWhiteSpace(programCode))
        {
            var normalizedProgram = programCode.Trim().ToUpperInvariant();
            events = events.Where(x => x.ProgramCode == normalizedProgram);
        }
        if (!string.IsNullOrWhiteSpace(action))
        {
            var normalizedAction = action.Trim().ToUpperInvariant();
            events = events.Where(x => x.Action == normalizedAction);
        }

        var joined = from ev in events
                     join user in db.Users.AsNoTracking() on ev.ActorUserId equals user.Id into userGroup
                     from user in userGroup.DefaultIfEmpty()
                     select new
                     {
                         ev,
                         ActorUserName = user != null ? (user.UserName ?? "") : ""
                     };

        var kw = keyword?.Trim();
        if (!string.IsNullOrEmpty(kw))
        {
            var upper = kw.ToUpperInvariant();
            joined = joined.Where(x =>
                x.ev.ProgramCode.Contains(upper)
                || x.ev.Action.ToUpper().Contains(upper)
                || x.ev.Result.ToUpper().Contains(upper)
                || x.ev.TraceId.ToUpper().Contains(upper)
                || x.ActorUserName.ToUpper().Contains(upper));
        }

        var ascending = sortDirection == "asc";
        var ordered = (sortField, ascending) switch
        {
            ("programCode", true) => joined.OrderBy(x => x.ev.ProgramCode),
            ("programCode", false) => joined.OrderByDescending(x => x.ev.ProgramCode),
            ("action", true) => joined.OrderBy(x => x.ev.Action),
            ("action", false) => joined.OrderByDescending(x => x.ev.Action),
            ("result", true) => joined.OrderBy(x => x.ev.Result),
            ("result", false) => joined.OrderByDescending(x => x.ev.Result),
            (_, true) => joined.OrderBy(x => x.ev.OccurredAtUtc),
            _ => joined.OrderByDescending(x => x.ev.OccurredAtUtc)
        };
        var stable = ascending ? ordered.ThenBy(x => x.ev.Id) : ordered.ThenByDescending(x => x.ev.Id);

        return stable.Select(x => new AuditEventItemDto(
            x.ev.Id.ToString(CultureInfo.InvariantCulture),
            x.ev.OccurredAtUtc,
            x.ev.ActorUserId.HasValue ? x.ev.ActorUserId.Value.ToString(CultureInfo.InvariantCulture) : null,
            x.ActorUserName,
            x.ev.CompanyId.HasValue ? x.ev.CompanyId.Value.ToString(CultureInfo.InvariantCulture) : null,
            x.ev.EntityId.HasValue ? x.ev.EntityId.Value.ToString(CultureInfo.InvariantCulture) : null,
            x.ev.ProgramCode,
            x.ev.Action,
            x.ev.Result,
            x.ev.TraceId));
    }

    private async Task<bool> IsAuditEventWithinNarrowScopeAsync(
        AuditEvent ev,
        ScopeFilter scope,
        CancellationToken cancellationToken)
    {
        if (ev.CompanyId is null || ev.EntityId is null) return false;
        var entityId = ev.EntityId.Value;
        if (ev.ProgramCode == "DBRT02")
            return scope.BranchIds.Contains(entityId);
        if (ev.ProgramCode == "DBRT04")
        {
            var divIds = await organizations.DivisionIdsByBranchesAsync(ev.CompanyId.Value, scope.BranchIds, cancellationToken);
            return divIds.Contains(entityId);
        }
        if (ev.ProgramCode == "DBRT03")
        {
            var branchDeptIds = await organizations.DepartmentIdsByBranchesAsync(ev.CompanyId.Value, scope.BranchIds, cancellationToken);
            return branchDeptIds.Contains(entityId) || scope.DepartmentIds.Contains(entityId);
        }
        return false;
    }

    private static string NormalizeAndValidateSettingValue(string dataType, string value)
    {
        var trimmed = (value ?? "").Trim();
        switch (dataType)
        {
            case "INTEGER":
                if (!long.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                    throw new RuleException("INVALID_SETTING_VALUE", "ค่าตั้งค่าชนิด INTEGER ต้องเป็นจำนวนเต็ม", 400);
                return trimmed;
            case "BOOLEAN":
                if (string.Equals(trimmed, "true", StringComparison.OrdinalIgnoreCase)) return "true";
                if (string.Equals(trimmed, "false", StringComparison.OrdinalIgnoreCase)) return "false";
                throw new RuleException("INVALID_SETTING_VALUE", "ค่าตั้งค่าชนิด BOOLEAN ต้องเป็น true หรือ false", 400);
            default:
                return trimmed;
        }
    }

    private static string CurrentPeriodKey(string resetPolicy, DateTimeOffset now) => resetPolicy switch
    {
        "YEAR" => now.UtcDateTime.ToString("yyyy", CultureInfo.InvariantCulture),
        "YEAR_MONTH" => now.UtcDateTime.ToString("yyyyMM", CultureInfo.InvariantCulture),
        _ => ""
    };

    private static string FormatDocumentNumber(string prefix, string periodKey, long sequenceNumber, int paddingLength)
    {
        var number = sequenceNumber.ToString(CultureInfo.InvariantCulture).PadLeft(paddingLength, '0');
        return string.IsNullOrEmpty(periodKey) ? $"{prefix}-{number}" : $"{prefix}-{periodKey}-{number}";
    }

    private static string EscapeCsvCell(string? raw)
    {
        if (string.IsNullOrEmpty(raw)) return "\"\"";
        var cleaned = raw.Replace("\r\n", " ", StringComparison.Ordinal)
            .Replace('\r', ' ')
            .Replace('\n', ' ');
        if (cleaned[0] is '=' or '+' or '-' or '@' or '\t')
            cleaned = "'" + cleaned;
        return "\"" + cleaned.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }

    private static MenuItemSnapshot ToMenuItemSnapshot(MenuItem item) =>
        new(item.ProgramCode, item.DisplayName, item.SortOrder, item.IsActive);

    private static MenuItemDto ToMenuItemDto(MenuItem item)
    {
        var def = ProgramRegistry.Programs.FirstOrDefault(p => p.Code == item.ProgramCode);
        return new MenuItemDto(
            item.Id.ToString(CultureInfo.InvariantCulture),
            item.ModuleCode,
            item.ProgramCode,
            def?.Name ?? item.DisplayName,
            item.DisplayName,
            item.Route,
            item.RequiredPermission,
            item.SortOrder,
            item.IsActive,
            item.Version.ToString(CultureInfo.InvariantCulture));
    }

    private static DocumentSequenceSnapshot ToDocumentSequenceSnapshot(DocumentSequence entity) => new(
        entity.Code,
        entity.Name,
        entity.CompanyId.ToString(CultureInfo.InvariantCulture),
        entity.Prefix,
        entity.ResetPolicy,
        entity.PaddingLength,
        entity.CurrentPeriod,
        entity.LastNumber,
        entity.IsActive);

    private static DocumentSequenceDto ToDocumentSequenceDto(DocumentSequence entity, DateTimeOffset now)
    {
        var periodKey = CurrentPeriodKey(entity.ResetPolicy, now);
        var nextNumber = string.Equals(entity.CurrentPeriod, periodKey, StringComparison.Ordinal)
            ? entity.LastNumber + 1
            : 1;
        return new DocumentSequenceDto(
            entity.Id.ToString(CultureInfo.InvariantCulture),
            entity.CompanyId.ToString(CultureInfo.InvariantCulture),
            entity.Code,
            entity.Name,
            entity.Prefix,
            entity.ResetPolicy,
            entity.PaddingLength,
            entity.CurrentPeriod,
            entity.LastNumber,
            FormatDocumentNumber(entity.Prefix, periodKey, nextNumber, entity.PaddingLength),
            entity.IsActive,
            entity.CodeLocked,
            entity.Version.ToString(CultureInfo.InvariantCulture));
    }

    private static ApprovalWorkflowSnapshot ToWorkflowSnapshot(
        ApprovalWorkflow workflow,
        IEnumerable<ApprovalWorkflowStep> steps) => new(
            workflow.Code,
            workflow.Name,
            workflow.CompanyId.ToString(CultureInfo.InvariantCulture),
            workflow.DocumentType,
            workflow.IsActive,
            steps.OrderBy(s => s.StepOrder).Select(s => new ApprovalWorkflowStepSnapshot(
                s.StepOrder,
                s.StepName,
                s.ApproverRoleId.ToString(CultureInfo.InvariantCulture),
                s.RequireDifferentActor)).ToArray());

    private static SystemSettingSnapshot ToSystemSettingSnapshot(SystemSetting entity) => new(
        entity.Key,
        entity.DisplayName,
        entity.CompanyId?.ToString(CultureInfo.InvariantCulture),
        entity.DataType,
        entity.IsSensitive ? (string.IsNullOrEmpty(entity.Value) ? "" : "***MASKED***") : entity.Value,
        entity.IsSensitive);

    private static SystemSettingDto ToSystemSettingDto(SystemSetting entity) => new(
        entity.Id.ToString(CultureInfo.InvariantCulture),
        entity.CompanyId?.ToString(CultureInfo.InvariantCulture),
        entity.Key,
        entity.DisplayName,
        entity.DataType,
        entity.IsSensitive ? "" : entity.Value,
        entity.IsSensitive,
        !string.IsNullOrEmpty(entity.Value),
        entity.Version.ToString(CultureInfo.InvariantCulture));
}

