using EmpPortal.Application.Security;

namespace EmpPortal.Application.UnitTests;

public sealed class SessionPolicyTests
{
    [Fact]
    public void DefaultPolicySatisfiesSecurityBaseline()
    {
        SessionPolicy policy = SessionPolicy.Default;

        policy.EnsureValid();

        Assert.Equal(TimeSpan.FromHours(3), policy.AbsoluteLifetime);
        Assert.Equal(TimeSpan.FromMinutes(30), policy.IdleTimeout);
        Assert.Equal(3, policy.MaxConcurrentSessions);
        Assert.Equal(TimeSpan.FromMinutes(5), policy.AccessTokenLifetime);
        Assert.Equal(TimeSpan.FromMinutes(1), policy.DirectoryRevalidationInterval);
    }
}
