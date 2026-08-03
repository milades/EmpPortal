namespace EmpPortal.Web.Security;

internal sealed class ProductionDataProtectionOptions
{
    public const string SectionName = "DataProtection";

    public string KeyRingPath { get; set; } = string.Empty;

    public string CertificateThumbprint { get; set; } = string.Empty;
}
