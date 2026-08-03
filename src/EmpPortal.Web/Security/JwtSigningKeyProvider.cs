using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.IdentityModel.Tokens;

namespace EmpPortal.Web.Security;

internal sealed class JwtSigningKeyProvider : IDisposable
{
    private readonly RSA? _developmentRsa;
    private readonly X509Certificate2? _signingCertificate;

    public JwtSigningKeyProvider(IHostEnvironment environment, JwtOptions options)
    {
        if (environment.IsDevelopment())
        {
            _developmentRsa = RSA.Create(3072);
            SecurityKey = new RsaSecurityKey(_developmentRsa)
            {
                KeyId = "development-ephemeral"
            };
        }
        else
        {
            _signingCertificate = CertificateStoreLoader.LoadFromLocalMachine(
                options.SigningCertificateThumbprint,
                requirePrivateKey: true,
                "Jwt:SigningCertificateThumbprint");
            SecurityKey = new X509SecurityKey(_signingCertificate);
        }

        SigningCredentials = new SigningCredentials(
            SecurityKey,
            SecurityAlgorithms.RsaSha256);
    }

    public SecurityKey SecurityKey { get; }

    public SigningCredentials SigningCredentials { get; }

    public void Dispose()
    {
        _developmentRsa?.Dispose();
        _signingCertificate?.Dispose();
    }

}
