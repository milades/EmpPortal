namespace EmpPortal.Web.Security;

internal sealed class PortalSessionOptions
{
    public const string SectionName = "Session";

    public int AbsoluteMinutes { get; set; } = 180;

    public int IdleMinutes { get; set; } = 30;

    public int MaxConcurrentPerUser { get; set; } = 3;

    public int AdRevalidationSeconds { get; set; } = 60;
}
