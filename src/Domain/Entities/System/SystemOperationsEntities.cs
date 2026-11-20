namespace clean.Domain.Entities.System;

public sealed class MenuItem
{
    public long Id { get; set; }
    public string ModuleCode { get; set; } = "";
    public string ProgramCode { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Route { get; set; } = "";
    public string RequiredPermission { get; set; } = "";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public long Version { get; set; } = 1;
}

public sealed class DocumentSequence
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Prefix { get; set; } = "";
    public string ResetPolicy { get; set; } = "NONE";
    public int PaddingLength { get; set; } = 5;
    public string CurrentPeriod { get; set; } = "";
    public long LastNumber { get; set; }
    public bool IsActive { get; set; } = true;
    public bool CodeLocked { get; set; }
    public long Version { get; set; } = 1;
}

public sealed class ApprovalWorkflow
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string DocumentType { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public bool CodeLocked { get; set; }
    public long Version { get; set; } = 1;
}

public sealed class ApprovalWorkflowStep
{
    public long Id { get; set; }
    public long WorkflowId { get; set; }
    public int StepOrder { get; set; }
    public string StepName { get; set; } = "";
    public long ApproverRoleId { get; set; }
    public bool RequireDifferentActor { get; set; } = true;
}

public sealed class SystemSetting
{
    public long Id { get; set; }
    public long? CompanyId { get; set; }
    public string Key { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string DataType { get; set; } = "STRING";
    public string Value { get; set; } = "";
    public bool IsSensitive { get; set; }
    public long Version { get; set; } = 1;
}

