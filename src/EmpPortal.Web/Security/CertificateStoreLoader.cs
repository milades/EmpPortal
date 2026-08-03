using System.Security.Cryptography.X509Certificates;

namespace EmpPortal.Web.Security;

internal static class CertificateStoreLoader
{
    public static X509Certificate2 LoadFromLocalMachine(
        string thumbprint,
        bool requirePrivateKey,
        string settingName)
    {
        if (string.IsNullOrWhiteSpace(thumbprint))
        {
            throw new InvalidOperationException($"{settingName} must be configured.");
        }

        using X509Store store = new(StoreName.My, StoreLocation.LocalMachine);
        store.Open(OpenFlags.ReadOnly);
        X509Certificate2Collection certificates = store.Certificates.Find(
            X509FindType.FindByThumbprint,
            thumbprint.Replace(" ", string.Empty, StringComparison.Ordinal),
            validOnly: true);
        X509Certificate2? certificate = certificates
            .OfType<X509Certificate2>()
            .SingleOrDefault(candidate => !requirePrivateKey || candidate.HasPrivateKey);

        return certificate ?? throw new InvalidOperationException(
            $"Certificate configured by {settingName} was not found or is invalid.");
    }
}
