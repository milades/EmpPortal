using EmpPortal.Domain.Access;
using EmpPortal.Domain.Hr;

namespace EmpPortal.Domain.UnitTests;

public sealed class PortalAccessGrantTests
{
    [Fact]
    public void CreateNormalizesEveryoneSubject()
    {
        PortalAccessGrant grant = PortalAccessGrant.Create(
            "benefits.view",
            PortalAccessSubjectType.Everyone,
            "ignored",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);

        Assert.Equal("*", grant.SubjectKey);
        Assert.Equal(PortalAccessSubjectType.Everyone, grant.SubjectType);
    }
}

public sealed class PayslipPeriodSettingTests
{
    [Fact]
    public void CreateStoresVisibility()
    {
        Guid actorId = Guid.NewGuid();
        PayslipPeriodSetting setting = PayslipPeriodSetting.Create(
            1404,
            5,
            isVisibleToEmployees: true,
            actorId,
            DateTimeOffset.UtcNow);

        Assert.True(setting.IsVisibleToEmployees);
        Assert.Equal(1404, setting.PersianYear);
        Assert.Equal(5, setting.PersianMonth);
    }

    [Fact]
    public void CreateRejectsInvalidMonth()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PayslipPeriodSetting.Create(1404, 13, true, Guid.NewGuid(), DateTimeOffset.UtcNow));
    }
}
