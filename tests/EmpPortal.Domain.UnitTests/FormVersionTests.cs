using EmpPortal.Domain.Forms;

namespace EmpPortal.Domain.UnitTests;

public sealed class FormVersionTests
{
    [Fact]
    public void PublishedVersionIsImmutable()
    {
        Guid actorId = Guid.NewGuid();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        FormVersion version = FormVersion.CreateDraft(
            Guid.NewGuid(),
            1,
            "{\"schemaVersion\":1}",
            "hash-1",
            actorId,
            now);

        version.Publish(actorId, now.AddMinutes(1));

        Assert.Equal(FormVersionStatus.Published, version.Status);
        Assert.Throws<InvalidOperationException>(() =>
            version.ReplaceDefinition(
                "{\"schemaVersion\":2}",
                "hash-2",
                actorId,
                now.AddMinutes(2)));
    }
}
