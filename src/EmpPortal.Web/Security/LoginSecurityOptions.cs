namespace EmpPortal.Web.Security;

internal sealed class LoginSecurityOptions
{
    public const string SectionName = "Login";

    public int AttemptLimit { get; set; } = 5;

    public int AttemptWindowMinutes { get; set; } = 15;
}
