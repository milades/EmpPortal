namespace EmpPortal.Infrastructure.Identity;

public sealed class BootstrapAdministratorOptions
{
    public const string SectionName = "BootstrapAdministrator";

    public string Upn { get; set; } = string.Empty;
}
