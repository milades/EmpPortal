namespace EmpPortal.Web.Security;

internal sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "EmpPortal";

    public string Audience { get; set; } = "EmpPortal.Api";

    public int AccessTokenMinutes { get; set; } = 5;

    public string SigningCertificateThumbprint { get; set; } = string.Empty;
}
