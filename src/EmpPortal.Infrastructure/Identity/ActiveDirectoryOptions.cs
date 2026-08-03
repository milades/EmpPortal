namespace EmpPortal.Infrastructure.Identity;

public sealed class ActiveDirectoryOptions
{
    public const string SectionName = "ActiveDirectory";

    public string DomainFqdn { get; set; } = string.Empty;

    public string BaseDn { get; set; } = string.Empty;

    public string[] DomainControllers { get; set; } = [];

    public int LdapsPort { get; set; } = 636;

    public int OperationTimeoutSeconds { get; set; } = 10;
}
