namespace EmpPortal.Infrastructure.Tabular;

public sealed class ExternalTabularSourceOptions
{
    public const string SectionName = "ExternalData";

    public ExternalSqlViewOptions Benefits { get; set; } = new();

    public ExternalSqlViewOptions Assets { get; set; } = new();
}

public sealed class ExternalSqlViewOptions
{
    public string ConnectionString { get; set; } = string.Empty;

    public string ViewName { get; set; } = string.Empty;

    public string PersonnelCodeColumn { get; set; } = "PersonnelCode";
}
