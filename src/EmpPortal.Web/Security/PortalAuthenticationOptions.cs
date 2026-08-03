namespace EmpPortal.Web.Security;

internal sealed class PortalAuthenticationOptions
{
    public const string SectionName = "Authentication";

    public bool SsoEnabled { get; set; } = true;

    public bool ManualLoginEnabled { get; set; } = true;
}
