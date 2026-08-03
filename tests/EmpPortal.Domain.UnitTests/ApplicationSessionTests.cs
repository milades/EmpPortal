using EmpPortal.Domain.Sessions;

namespace EmpPortal.Domain.UnitTests;

public sealed class ApplicationSessionTests
{
    [Fact]
    public void RevokeMakesSessionInactive()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        ApplicationSession session = ApplicationSession.Create(
            Guid.NewGuid(),
            now,
            TimeSpan.FromHours(3),
            TimeSpan.FromMinutes(30));

        session.Revoke(now.AddMinutes(1), "directory-account-disabled");

        Assert.True(session.IsRevoked);
        Assert.False(session.IsActiveAt(now.AddMinutes(2)));
    }
}
