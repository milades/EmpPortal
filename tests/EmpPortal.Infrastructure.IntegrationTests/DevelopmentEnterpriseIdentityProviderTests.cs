using EmpPortal.Application.Identity;
using EmpPortal.Infrastructure.Identity;

namespace EmpPortal.Infrastructure.IntegrationTests;

public sealed class DevelopmentEnterpriseIdentityProviderTests
{
    [Fact]
    public async Task AuthenticatePasswordAsyncReturnsConfiguredDevelopmentIdentity()
    {
        DevelopmentDirectoryAccount account = new(
            Guid.NewGuid(),
            "S-1-5-21-1",
            "admin@empportal.test",
            "مدیر آزمایشی",
            null);
        DevelopmentEnterpriseIdentityProvider provider = new([account], true);

        PasswordAuthenticationResult result = await provider.AuthenticatePasswordAsync(
            "ADMIN@EMPPORTAL.TEST",
            "any-non-empty-development-password");

        Assert.True(result.Succeeded);
        Assert.Equal("admin@empportal.test", result.Identity!.Upn);
    }

    [Fact]
    public async Task FindByLoginNameAsyncSupportsSsoUpn()
    {
        DevelopmentDirectoryAccount account = new(
            Guid.NewGuid(),
            "S-1-5-21-2",
            "employee@empportal.test",
            "کارمند آزمایشی",
            null);
        DevelopmentEnterpriseIdentityProvider provider = new([account], true);

        EnterpriseIdentity? identity = await provider.FindByLoginNameAsync(
            "EMPLOYEE@EMPPORTAL.TEST");

        Assert.NotNull(identity);
        Assert.Equal(account.ObjectGuid, identity.ObjectGuid);
    }
}
