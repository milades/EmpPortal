using EmpPortal.Domain.Configuration;

namespace EmpPortal.Domain.UnitTests;

public sealed class RuntimeSettingTests
{
    [Fact]
    public void UpdateChangesValueAndAuditMetadata()
    {
        Guid firstAdministrator = Guid.NewGuid();
        Guid secondAdministrator = Guid.NewGuid();
        DateTimeOffset createdAt = new(2026, 8, 3, 10, 0, 0, TimeSpan.Zero);
        RuntimeSetting setting = RuntimeSetting.Create(
            "Session:IdleMinutes",
            "30",
            createdAt,
            firstAdministrator);

        setting.Update("45", createdAt.AddMinutes(2), secondAdministrator);

        Assert.Equal("45", setting.Value);
        Assert.Equal(secondAdministrator, setting.UpdatedByUserId);
        Assert.Equal(createdAt.AddMinutes(2), setting.UpdatedAtUtc);
    }
}
