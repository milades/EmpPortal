namespace EmpPortal.Application.Forms.Schema;

public sealed class FormSchemaDefinition
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string SubmitButtonText { get; set; } = "ثبت نهایی";

    public List<FormPageDefinition> Pages { get; set; } = [];
}

public sealed class FormPageDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public List<FormSectionDefinition> Sections { get; set; } = [];
}

public sealed class FormSectionDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string? Title { get; set; }

    public string? Description { get; set; }

    public int Columns { get; set; } = 12;

    public List<FormElementDefinition> Elements { get; set; } = [];
}

public sealed class FormElementDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Key { get; set; } = string.Empty;

    public FormElementType Type { get; set; }

    public string Label { get; set; } = string.Empty;

    public string? HelpText { get; set; }

    public string? Placeholder { get; set; }

    public string? Content { get; set; }

    public string? DefaultValue { get; set; }

    public int Width { get; set; } = 12;

    public bool Required { get; set; }

    public bool ReadOnly { get; set; }

    public FormValidationDefinition Validation { get; set; } = new();

    public List<FormOptionDefinition> Options { get; set; } = [];

    public FormConditionGroupDefinition? VisibilityCondition { get; set; }

    public string? CalculationExpression { get; set; }

    public List<FormElementDefinition> Children { get; set; } = [];
}

public sealed class FormOptionDefinition
{
    public string Value { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;
}

public sealed class FormValidationDefinition
{
    public int? MinimumLength { get; set; }

    public int? MaximumLength { get; set; }

    public decimal? MinimumValue { get; set; }

    public decimal? MaximumValue { get; set; }

    public string? Pattern { get; set; }

    public string? CustomErrorMessage { get; set; }
}

public sealed class FormConditionGroupDefinition
{
    public FormConditionLogic Logic { get; set; }

    public List<FormConditionRuleDefinition> Rules { get; set; } = [];
}

public sealed class FormConditionRuleDefinition
{
    public string FieldKey { get; set; } = string.Empty;

    public FormConditionOperator Operator { get; set; }

    public string? Value { get; set; }
}
